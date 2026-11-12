using System.Collections.Concurrent;
using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElMomenNursery.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(NurseryDbContext dbContext) : IUnitOfWork
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

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return dbContext.SaveChangesAsync(cancellationToken);
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
        catch
        {
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
}
