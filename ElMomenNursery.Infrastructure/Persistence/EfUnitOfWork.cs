using System.Collections.Concurrent;
using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ElMomenNursery.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(NurseryDbContext dbContext, ILogger<EfUnitOfWork> logger) : IUnitOfWork, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return (IRepository<TEntity>)_repositories.GetOrAdd(
            typeof(TEntity),
            _ => new EfRepository<TEntity>(dbContext));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var changesPending = dbContext.ChangeTracker.HasChanges();
        try
        {
            var affectedRows = await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogDebug("Saved {AffectedRows} database changes", affectedRows);
            if (changesPending && affectedRows == 0)
                logger.LogWarning("Database save reported no affected rows while changes were pending");
            return affectedRows;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning(exception, "Database concurrency conflict during save");
            throw;
        }
        catch (DbUpdateException exception) when (TryGetSqliteConstraintKind(exception, out var constraintKind))
        {
            logger.LogWarning(exception, "Database constraint rejected save; constraintKind={ConstraintKind}", constraintKind);
            throw;
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Database update failed");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to save database changes");
            throw;
        }
    }

    private static bool TryGetSqliteConstraintKind(DbUpdateException exception, out string constraintKind)
    {
        var sqliteException = exception.GetBaseException() as SqliteException;
        if (sqliteException?.SqliteErrorCode == 19)
        {
            constraintKind = sqliteException.SqliteExtendedErrorCode switch
            {
                1555 or 2067 => "UNIQUE",
                787 => "FOREIGN KEY",
                275 => "CHECK",
                _ => "CONSTRAINT"
            };
            return true;
        }

        constraintKind = string.Empty;
        return false;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active for this unit of work.");
        }

        _transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_transaction is null)
        {
            throw new InvalidOperationException("There is no active transaction to commit.");
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to commit database transaction; rolling back");
            await _transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        // DbContext is scoped and disposed by the dependency injection container.
        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _transaction?.Dispose();
        _transaction = null;
        // DbContext is scoped and disposed by the dependency injection container.
        _disposed = true;
    }
}
