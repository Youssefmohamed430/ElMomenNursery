using System.Linq.Expressions;
using ElMomenNursery.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElMomenNursery.Infrastructure.Persistence.Repositories;

internal sealed class EfRepository<TEntity>(NurseryDbContext dbContext) : IRepository<TEntity>
    where TEntity : class
{
    private DbSet<TEntity> Entities => dbContext.Set<TEntity>();

    public IQueryable<TEntity> Query(bool asNoTracking = true)
    {
        var query = Entities.AsQueryable();
        return asNoTracking ? query.AsNoTracking() : query;
    }

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return (asNoTracking ? Query() : Query(asNoTracking: false))
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? Query() : Query(asNoTracking: false);
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query.ToListAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Entities.AnyAsync(predicate, cancellationToken);
    }

    public async ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Entities.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Remove(entity);
    }
}
