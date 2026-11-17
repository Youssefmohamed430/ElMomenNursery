using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ElMomenNursery.Infrastructure.Persistence;

public sealed class SoftDeleteInterceptor(ISessionManager sessionManager) : SaveChangesInterceptor
{
    private void ConvertDeletes(DbContext? context)
    {
        if (context is null) return;
        var now = DateTimeOffset.UtcNow;
        var actor = sessionManager.CurrentUser?.UserId;
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>().Where(x => x.State == EntityState.Deleted).ToArray())
        {
            if (entry.Entity is Role { IsSystemRole: true })
            {
                entry.State = EntityState.Unchanged;
                continue;
            }
            var entityType = entry.Metadata.ClrType.Name;
            var entityId = entry.Property("Id").CurrentValue?.ToString();
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
            entry.Entity.DeletedByUserId = actor;
            context.Set<AuditLog>().Add(new AuditLog
            {
                UserAccountId = actor, Action = $"{entityType}.Deleted", EntityType = entityType,
                EntityId = entityId, ChangeSummary = "تم نقل السجل إلى المحذوفات"
            });
        }
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ConvertDeletes(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ConvertDeletes(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
