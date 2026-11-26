using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;

namespace ElMomenNursery.Application.Services;

public sealed class LookupService(IUnitOfWork uow, IPermissionService permissions, ISessionManager sessions) : ILookupService
{
    public async Task<Result<IReadOnlyList<LookupItemDto>>> GetAsync(string category, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        if (!permissions.HasPermission("Lookups.View")) return new Error("Lookups.PermissionDenied", "لا تملك صلاحية عرض القوائم.");
        var rows = await uow.Repository<LookupItem>().ListAsync(x => x.Category == category && (includeInactive || x.IsActive), cancellationToken: cancellationToken);
        return Result.Success<IReadOnlyList<LookupItemDto>>(rows.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(ToDto).ToArray());
    }
    public async Task<Result<LookupItemDto>> CreateAsync(SaveLookupItemDto dto, CancellationToken cancellationToken = default)
    {
        if (!permissions.HasPermission("Lookups.Manage")) return new Error("Lookups.PermissionDenied", "لا تملك صلاحية إدارة القوائم.");
        var validation = Validate(dto); if (validation is not null) return validation;
        var repo = uow.Repository<LookupItem>();
        if (await repo.AnyAsync(x => x.Category == dto.Category && x.Name == dto.Name.Trim(), cancellationToken)) return new Error("Lookups.Duplicate", "هذا الاسم موجود بالفعل في القائمة.");
        var item = new LookupItem { Category = dto.Category.Trim(), Name = dto.Name.Trim(), SortOrder = dto.SortOrder, IsActive = dto.IsActive };
        await repo.AddAsync(item, cancellationToken); await AuditAsync("Lookup.Created", item, cancellationToken); await uow.SaveChangesAsync(cancellationToken); return Result.Success(ToDto(item), "تمت إضافة العنصر.");
    }
    public async Task<Result<LookupItemDto>> UpdateAsync(Guid id, SaveLookupItemDto dto, CancellationToken cancellationToken = default)
    {
        if (!permissions.HasPermission("Lookups.Manage")) return new Error("Lookups.PermissionDenied", "لا تملك صلاحية إدارة القوائم.");
        var validation = Validate(dto); if (validation is not null) return validation;
        var repo = uow.Repository<LookupItem>(); var item = await repo.FirstOrDefaultAsync(x => x.Id == id, asNoTracking: false, cancellationToken: cancellationToken);
        if (item is null) return new Error("Lookups.NotFound", "العنصر غير موجود.");
        if (await repo.AnyAsync(x => x.Id != id && x.Category == dto.Category && x.Name == dto.Name.Trim(), cancellationToken)) return new Error("Lookups.Duplicate", "هذا الاسم موجود بالفعل في القائمة.");
        item.Name = dto.Name.Trim(); item.SortOrder = dto.SortOrder; item.IsActive = dto.IsActive; await AuditAsync("Lookup.Updated", item, cancellationToken); await uow.SaveChangesAsync(cancellationToken); return Result.Success(ToDto(item), "تم تحديث العنصر.");
    }
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!permissions.HasPermission("Lookups.Manage")) return new Error("Lookups.PermissionDenied", "لا تملك صلاحية إدارة القوائم.");
        var item = await uow.Repository<LookupItem>().FirstOrDefaultAsync(x => x.Id == id, asNoTracking: false, cancellationToken: cancellationToken);
        if (item is null) return new Error("Lookups.NotFound", "العنصر غير موجود.");
        uow.Repository<LookupItem>().Remove(item); await AuditAsync("Lookup.Deleted", item, cancellationToken); await uow.SaveChangesAsync(cancellationToken); return Result.Success("تم نقل العنصر إلى المحذوفات.");
    }
    private static Error? Validate(SaveLookupItemDto dto) => string.IsNullOrWhiteSpace(dto.Category) || string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 120 ? new Error("Lookups.Invalid", "أدخل اسمًا صالحًا للقائمة.") : null;
    private static LookupItemDto ToDto(LookupItem x) => new(x.Id, x.Category, x.Name, x.SortOrder, x.IsActive, x.IsSystem);
    private async Task AuditAsync(string action, LookupItem item, CancellationToken ct) => await uow.Repository<AuditLog>().AddAsync(new AuditLog { Action = action, EntityType = nameof(LookupItem), EntityId = item.Id.ToString("D"), UserAccountId = sessions.CurrentUser?.UserId, ChangeSummary = $"{item.Category}: {item.Name}" }, ct);
}
