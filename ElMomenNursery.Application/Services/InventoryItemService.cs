namespace ElMomenNursery.Application.Services;

public sealed class InventoryItemService(IUnitOfWork unitOfWork, ILogger<InventoryItemService> logger, IPermissionService permissions, ISessionManager sessions) : IInventoryItemService
{
    public async Task<Result<IReadOnlyList<InventoryItemDto>>> GetItemsAsync(InventoryItemFilter filter, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.View")) return InventoryErrors.PermissionDenied;
        var rows = await unitOfWork.Repository<InventoryItem>().Query().Include(x => x.Category).ToListAsync(cancellationToken);
        var search = ArabicNameNormalizer.Normalize(filter.SearchText);
        var result = rows.Where(x => (filter.CategoryId is null || x.CategoryId == filter.CategoryId)
            && (filter.IsActive is null || x.IsActive == filter.IsActive)
            && (search.Length == 0 || ArabicNameNormalizer.Normalize(x.Name).Contains(search, StringComparison.Ordinal))
            && (filter.MinPrice is null || x.UnitPrice >= filter.MinPrice)
            && (filter.MaxPrice is null || x.UnitPrice <= filter.MaxPrice))
            .OrderBy(x => x.Category.Name, StringComparer.CurrentCulture).ThenBy(x => x.Name, StringComparer.CurrentCulture).Select(ToDto).ToArray();
        return Result.Success<IReadOnlyList<InventoryItemDto>>(result);
    }

    public async Task<Result<InventoryItemDto>> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.View")) return InventoryErrors.PermissionDenied;
        var item = await unitOfWork.Repository<InventoryItem>().Query().Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == itemId, cancellationToken);
        return item is null ? InventoryErrors.ItemNotFound : Result.Success(ToDto(item));
    }

    public async Task<Result<InventoryItemDto>> CreateItemAsync(CreateItemDto dto, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Manage")) return InventoryErrors.PermissionDenied;
        var error = Validate(dto.Name, dto.UnitPrice, dto.Quantity); if (error is not null) return error;
        var category = await unitOfWork.Repository<InventoryCategory>().FirstOrDefaultAsync(x => x.Id == dto.CategoryId, asNoTracking: false, cancellationToken: cancellationToken);
        if (category is null) return InventoryErrors.CategoryNotFound;
        if (!category.IsActive) return InventoryErrors.CategoryInactive;
        var name = dto.Name.Trim();
        if (await HasDuplicate(name, category.Id, null, cancellationToken)) return InventoryErrors.DuplicateItemName;
        var item = new InventoryItem { Name = name, CategoryId = category.Id, Category = category, UnitPrice = dto.UnitPrice, Quantity = dto.Quantity, IsActive = dto.IsActive };
        try { await unitOfWork.Repository<InventoryItem>().AddAsync(item, cancellationToken); await Audit("InventoryItem.Created", item.Id, "—", Values(item), cancellationToken); await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Inventory item create failed; errorCode={ErrorCode}", PersistenceErrorMapper.FromException(ex).Code); return PersistenceErrorMapper.FromException(ex); }
        return Result.Success(ToDto(item), InventoryErrors.ItemCreated.Description);
    }

    public async Task<Result<InventoryItemDto>> UpdateItemAsync(Guid itemId, UpdateItemDto dto, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Manage")) return InventoryErrors.PermissionDenied;
        var error = Validate(dto.Name, dto.UnitPrice, dto.Quantity); if (error is not null) return error;
        try
        {
            var item = await unitOfWork.Repository<InventoryItem>().Query(asNoTracking: false).Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == itemId, cancellationToken);
            if (item is null) return InventoryErrors.ItemNotFound;
            if (dto.ExpectedQuantity is int expected && item.Quantity != expected) return InventoryErrors.QuantityChanged;
            if (dto.Quantity != item.Quantity && string.IsNullOrWhiteSpace(dto.QuantityAdjustmentReason)) return InventoryErrors.QuantityAdjustmentReasonRequired;
            var category = await unitOfWork.Repository<InventoryCategory>().FirstOrDefaultAsync(x => x.Id == dto.CategoryId, asNoTracking: false, cancellationToken: cancellationToken);
            if (category is null) return InventoryErrors.CategoryNotFound;
            if (dto.CategoryId != item.CategoryId && !category.IsActive) return InventoryErrors.CategoryInactive;
            if (await HasDuplicate(dto.Name.Trim(), category.Id, item.Id, cancellationToken)) return InventoryErrors.DuplicateItemName;
            var before = Values(item);
            var quantityDelta = dto.Quantity - item.Quantity;
            item.Name = dto.Name.Trim(); item.CategoryId = category.Id; item.Category = category; item.UnitPrice = dto.UnitPrice; item.Quantity = dto.Quantity; item.IsActive = dto.IsActive;
            if (quantityDelta != 0)
                await unitOfWork.Repository<StockMovement>().AddAsync(new StockMovement
                {
                    InventoryItemId = item.Id, InventoryItem = item, Quantity = quantityDelta,
                    MovementType = ElMomenNursery.Domain.Enums.StockMovementType.Adjustment,
                    OccurredAt = DateTimeOffset.UtcNow, RecordedByUserId = sessions.CurrentUser?.UserId,
                    Notes = dto.QuantityAdjustmentReason!.Trim()
                }, cancellationToken);
            await Audit("InventoryItem.Updated", item.Id, before, Values(item), cancellationToken); await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(ToDto(item), InventoryErrors.ItemUpdated.Description);
        }
        catch (DbUpdateConcurrencyException ex) { logger.LogWarning(ex, "Inventory item quantity changed concurrently; itemId={ItemId}", itemId); return InventoryErrors.QuantityChanged; }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Inventory item update failed; itemId={ItemId}; errorCode={ErrorCode}", itemId, PersistenceErrorMapper.FromException(ex).Code); return PersistenceErrorMapper.FromException(ex); }
    }

    public async Task<Result> SetItemActiveAsync(Guid itemId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Manage")) return InventoryErrors.PermissionDenied;
        var item = await unitOfWork.Repository<InventoryItem>().FirstOrDefaultAsync(x => x.Id == itemId, false, cancellationToken: cancellationToken);
        if (item is null) return InventoryErrors.ItemNotFound;
        var before = Values(item); item.IsActive = isActive; await Audit("InventoryItem.StatusUpdated", item.Id, before, Values(item), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken); return Result.Success(InventoryErrors.StatusUpdated.Description);
    }

    public async Task<Result> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Manage")) return InventoryErrors.PermissionDenied;
        var item = await unitOfWork.Repository<InventoryItem>().FirstOrDefaultAsync(x => x.Id == itemId, false, cancellationToken: cancellationToken);
        if (item is null) return InventoryErrors.ItemNotFound;
        unitOfWork.Repository<InventoryItem>().Remove(item);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); return Result.Success(InventoryErrors.ItemDeleted.Description); }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Inventory item delete failed; itemId={ItemId}", itemId); return PersistenceErrorMapper.FromException(ex); }
    }

    public async Task<Result<IReadOnlyList<InventoryItemDto>>> GetDeletedItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Restore")) return InventoryErrors.PermissionDenied;
        var rows = await unitOfWork.Repository<InventoryItem>().Query().IgnoreQueryFilters().Where(x => x.IsDeleted).Include(x => x.Category).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<InventoryItemDto>>(rows.Select(ToDto).OrderBy(x => x.CategoryName, StringComparer.CurrentCulture).ThenBy(x => x.Name, StringComparer.CurrentCulture).ToArray());
    }

    public async Task<Result> RestoreItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Restore")) return InventoryErrors.PermissionDenied;
        var item = await unitOfWork.Repository<InventoryItem>().Query(asNoTracking: false).IgnoreQueryFilters().Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == itemId && x.IsDeleted, cancellationToken);
        if (item is null) return InventoryErrors.ItemNotFound;
        var category = await unitOfWork.Repository<InventoryCategory>().Query().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == item.CategoryId, cancellationToken);
        if (category is null || category.IsDeleted) return InventoryErrors.CategoryDeleted;
        if (await HasDuplicate(item.Name, item.CategoryId, null, cancellationToken)) return InventoryErrors.DuplicateItemName;
        var before = "محذوف"; item.IsDeleted = false; item.DeletedAt = null; item.DeletedByUserId = null;
        await Audit("InventoryItem.Restored", item.Id, before, Values(item), cancellationToken); await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(InventoryErrors.ItemRestored.Description);
    }

    private bool Allowed(string code) => PermissionGuard.Check(permissions, code, InventoryErrors.PermissionDenied) is null;
    private static Error? Validate(string? name, decimal price, int quantity) => string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100 ? InventoryErrors.InvalidData : price < 0 || decimal.Round(price, 2) != price ? InventoryErrors.InvalidPrice : quantity < 0 ? InventoryErrors.InvalidQuantity : null;
    private async Task<bool> HasDuplicate(string name, Guid categoryId, Guid? excludeId, CancellationToken ct)
    {
        var normalized = ArabicNameNormalizer.Normalize(name);
        var rows = await unitOfWork.Repository<InventoryItem>().Query().Where(x => x.CategoryId == categoryId && (excludeId == null || x.Id != excludeId)).Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        return rows.Any(x => ArabicNameNormalizer.Normalize(x.Name) == normalized);
    }
    private async Task Audit(string action, Guid id, string before, string after, CancellationToken ct) => await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog { Action = action, EntityType = nameof(InventoryItem), EntityId = id.ToString("D"), UserAccountId = sessions.CurrentUser?.UserId, ChangeSummary = $"قبل: {before}; بعد: {after}; المستخدم: {sessions.CurrentUser?.FullName ?? "—"}" }, ct);
    private static string Values(InventoryItem x) => $"الاسم: {x.Name}; الفئة: {x.Category?.Name}; السعر: {x.UnitPrice:0.00}; الكمية: {x.Quantity} قطعة; الحالة: {(x.IsActive ? "نشط" : "موقوف")}";
    private static InventoryItemDto ToDto(InventoryItem x) => new() { Id = x.Id, Name = x.Name, CategoryId = x.CategoryId, CategoryName = x.Category?.Name ?? string.Empty, UnitPrice = x.UnitPrice, Quantity = x.Quantity, IsActive = x.IsActive, CategoryIsActive = x.Category?.IsActive ?? false };
}
