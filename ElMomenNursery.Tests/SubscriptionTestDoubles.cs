using System.Linq.Expressions;
using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElMomenNursery.Tests;

internal sealed class TestPermissionService(params string[] denied) : IPermissionService
{
    public bool HasPermission(string code) => !denied.Contains(code, StringComparer.Ordinal);
    public bool HasAnyPermission(params string[] codes) => codes.Any(HasPermission);
    public bool HasAllPermissions(params string[] codes) => codes.All(HasPermission);
    public Task RefreshCurrentUserAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class TestSessionManager(UserSession? current) : ISessionManager
{
    public UserSession? CurrentUser { get; private set; } = current;
    public bool IsAuthenticated => CurrentUser is not null;
    public void Login(UserSession session) => CurrentUser = session;
    public void Logout() => CurrentUser = null;
}

internal sealed class TestUnitOfWork(NurseryDbContext db) : IUnitOfWork
{
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;
    public IRepository<TEntity> Repository<TEntity>() where TEntity : class => new TestRepository<TEntity>(db);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) => _transaction = await db.Database.BeginTransactionAsync(cancellationToken);
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default) { await db.SaveChangesAsync(cancellationToken); if (_transaction is not null) { await _transaction.CommitAsync(cancellationToken); await _transaction.DisposeAsync(); _transaction = null; } }
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default) { if (_transaction is not null) { await _transaction.RollbackAsync(cancellationToken); await _transaction.DisposeAsync(); _transaction = null; } }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class TestRepository<TEntity>(NurseryDbContext db) : IRepository<TEntity> where TEntity : class
{
    private DbSet<TEntity> Set => db.Set<TEntity>();
    public IQueryable<TEntity> Query(bool asNoTracking = true) => asNoTracking ? Set.AsNoTracking() : Set;
    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = true, string[]? includeProperties = null, CancellationToken cancellationToken = default)
    { IQueryable<TEntity> query = Query(asNoTracking); foreach (var include in includeProperties ?? []) query = query.Include(include); return await query.FirstOrDefaultAsync(predicate, cancellationToken); }
    public async Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>>? predicate = null, bool asNoTracking = true, string[]? includeProperties = null, CancellationToken cancellationToken = default)
    { IQueryable<TEntity> query = Query(asNoTracking); if (predicate is not null) query = query.Where(predicate); foreach (var include in includeProperties ?? []) query = query.Include(include); return await query.ToListAsync(cancellationToken); }
    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) => Set.AnyAsync(predicate, cancellationToken);
    public async ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default) => await Set.AddAsync(entity, cancellationToken);
    public void Update(TEntity entity) => Set.Update(entity);
    public void Remove(TEntity entity) => Set.Remove(entity);
}
