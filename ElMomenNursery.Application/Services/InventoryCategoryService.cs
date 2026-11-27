namespace ElMomenNursery.Application.Services;

public sealed class InventoryCategoryService(IUnitOfWork unitOfWork, ILogger<InventoryCategoryService> logger, IPermissionService permissions, ISessionManager sessions) : IInventoryCategoryService
{
    public async Task<Result<IReadOnlyList<InventoryCategoryDto>>> GetCategoriesAsync(InventoryCategoryFilter filter, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.View")) return InventoryErrors.PermissionDenied;
        var rows = await unitOfWork.Repository<InventoryCategory>().Query().Include(x => x.Items).Include(x => x.ParentCategory).ToListAsync(cancellationToken);
        var search = ArabicNameNormalizer.Normalize(filter.SearchText);
        var selected = rows.Where(x => (filter.IsActive is null || x.IsActive == filter.IsActive) && (search.Length == 0 || ArabicNameNormalizer.Normalize(x.Name).Contains(search, StringComparison.Ordinal))).OrderBy(x => x.Name, StringComparer.CurrentCulture).ToArray();
        return Result.Success<IReadOnlyList<InventoryCategoryDto>>(selected.Select(x => ToDto(x)).ToArray());
    }
    public async Task<Result<InventoryCategoryDto>> GetCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.View")) return InventoryErrors.PermissionDenied;

        var x = await unitOfWork.Repository<InventoryCategory>()
            .Query()
            .Include(c => c.Items)
            .SingleOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        if (x is null) return InventoryErrors.CategoryNotFound;
        return Result.Success(ToDto(x));
    }
    public async Task<Result<IReadOnlyList<InventoryCategoryLookupDto>>> GetCategoryLookupAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.View")) return InventoryErrors.PermissionDenied;

        var rows = await unitOfWork.Repository<InventoryCategory>()
            .Query()
            .Include(x => x.ParentCategory)
            .ToListAsync(cancellationToken);

        var result = rows.Where(x => !activeOnly || x.IsActive)
                         .OrderBy(x => x.Name, StringComparer.CurrentCulture)
                         .Select(x => new InventoryCategoryLookupDto(x.Id, x.Name, x.IsActive, x.ParentCategoryId, x.ParentCategory?.Name))
                         .ToArray();

        return Result.Success<IReadOnlyList<InventoryCategoryLookupDto>>(result);
    }
    public async Task<Result<IReadOnlyList<InventoryCategoryNodeDto>>> GetCategoryTreeAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.View") && !Allowed("Payments.View")) return InventoryErrors.PermissionDenied;
        var categories = await unitOfWork.Repository<InventoryCategory>().Query()
            .Where(x => !activeOnly || x.IsActive).ToListAsync(cancellationToken);
        var ids = categories.Select(x => x.Id).ToHashSet();
        var items = await unitOfWork.Repository<InventoryItem>().Query().Include(x => x.Category)
            .Where(x => x.IsActive && x.Quantity > 0 && x.Category.IsActive && ids.Contains(x.CategoryId))
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var itemsByCategory = items.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key,
            g => (IReadOnlyList<InventoryItemDto>)g.Select(x => new InventoryItemDto { Id = x.Id, Name = x.Name, CategoryId = x.CategoryId, CategoryName = x.Category.Name, UnitPrice = x.UnitPrice, Quantity = x.Quantity, IsActive = x.IsActive, CategoryIsActive = x.Category.IsActive }).ToArray());
        var children = categories.ToLookup(x => x.ParentCategoryId);
        IReadOnlyList<InventoryCategoryNodeDto> Build(Guid? parentId, int depth) =>
            children[parentId].OrderBy(x => x.Name).Select(x => new InventoryCategoryNodeDto(x.Id, x.Name, x.IsActive, depth,
            Build(x.Id, depth + 1), children[x.Id].Any() ? null : itemsByCategory.GetValueOrDefault(x.Id))).ToArray();
        return Result.Success(Build(null, 0));
    }
    public async Task<Result<InventoryCategoryDto>> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Manage")) return InventoryErrors.PermissionDenied;

        var name = Clean(dto.Name);
        if (name is null || name.Length > 100 || (dto.Description?.Length ?? 0) > 500) return InventoryErrors.InvalidData;
        if (dto.ParentCategoryId is not null && !await ValidateParent(dto.ParentCategoryId, null, cancellationToken)) return InventoryErrors.InvalidParentCategory;

        if (await Duplicate(name, null, cancellationToken)) return InventoryErrors.DuplicateCategoryName;
        var entity = new InventoryCategory
        {
            Name = name,
            Description = Clean(dto.Description),
            IsActive = dto.IsActive,
            ParentCategoryId = dto.ParentCategoryId
        };

        try
        {
            await unitOfWork.Repository<InventoryCategory>().AddAsync(entity, cancellationToken);
            await Audit("InventoryCategory.Created", entity.Id, "—", Values(entity), cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Inventory category create failed; errorCode={ErrorCode}", PersistenceErrorMapper.FromException(ex).Code); return PersistenceErrorMapper.FromException(ex).Code == PersistenceErrors.Unique.Code ? InventoryErrors.DuplicateCategoryName : PersistenceErrorMapper.FromException(ex);
        }

        return Result.Success(ToDto(entity), InventoryErrors.CategoryCreated.Description);
    }
    public async Task<Result<InventoryCategoryDto>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Manage")) return InventoryErrors.PermissionDenied;

        var name = Clean(dto.Name); if (name is null || name.Length > 100 || (dto.Description?.Length ?? 0) > 500) return InventoryErrors.InvalidData;

        var entity = await unitOfWork.Repository<InventoryCategory>().Query(asNoTracking: false).Include(x => x.Items)
                                     .SingleOrDefaultAsync(x => x.Id == categoryId, cancellationToken);

        if (entity is null) return InventoryErrors.CategoryNotFound;
        if (await Duplicate(name, entity.Id, cancellationToken)) return InventoryErrors.DuplicateCategoryName;
        if (dto.ParentCategoryId is not null && !await ValidateParent(dto.ParentCategoryId, entity.Id, cancellationToken)) return InventoryErrors.InvalidParentCategory;
        var before = Values(entity); entity.Name = name; entity.Description = Clean(dto.Description); entity.IsActive = dto.IsActive; entity.ParentCategoryId = dto.ParentCategoryId;

        try
        {
            await Audit("InventoryCategory.Updated", entity.Id, before, Values(entity), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Inventory category update failed; categoryId={CategoryId}", categoryId); return PersistenceErrorMapper.FromException(ex);
        }

        return Result.Success(ToDto(entity), InventoryErrors.CategoryUpdated.Description);
    }
    public async Task<Result> SetCategoryActiveAsync(Guid categoryId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Manage")) return InventoryErrors.PermissionDenied;

        var entity = await unitOfWork.Repository<InventoryCategory>().FirstOrDefaultAsync(x => x.Id == categoryId, false, cancellationToken: cancellationToken); if (entity is null) return InventoryErrors.CategoryNotFound;

        var before = Values(entity);
        entity.IsActive = isActive;

        await Audit("InventoryCategory.StatusUpdated", entity.Id, before, Values(entity), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(InventoryErrors.StatusUpdated.Description);
    }
    public async Task<Result> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Manage")) return InventoryErrors.PermissionDenied;

        var entity = await unitOfWork.Repository<InventoryCategory>().FirstOrDefaultAsync(x => x.Id == categoryId, false, cancellationToken: cancellationToken); if (entity is null) return InventoryErrors.CategoryNotFound;

        if (await unitOfWork.Repository<InventoryCategory>().AnyAsync(x => x.ParentCategoryId == categoryId, cancellationToken)) return InventoryErrors.CategoryHasSubcategories;
        if (await unitOfWork.Repository<InventoryItem>().AnyAsync(x => x.CategoryId == categoryId, cancellationToken)) return InventoryErrors.CategoryHasItems;

        unitOfWork.Repository<InventoryCategory>().Remove(entity);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Inventory category delete failed; categoryId={CategoryId}", categoryId); return PersistenceErrorMapper.FromException(ex);
        }

        return Result.Success(InventoryErrors.CategoryDeletedMessage.Description);
    }
    public async Task<Result<IReadOnlyList<InventoryCategoryDto>>> GetDeletedCategoriesAsync(CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Restore")) return InventoryErrors.PermissionDenied;

        var rows = await unitOfWork.Repository<InventoryCategory>()
            .Query()
            .IgnoreQueryFilters()
            .Where(x => x.IsDeleted)
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<InventoryCategoryDto>>(rows.OrderBy(x => x.Name, StringComparer.CurrentCulture).Select(ToDto).ToArray());
    }
    public async Task<Result> RestoreCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Inventory.Restore")) return InventoryErrors.PermissionDenied;

        var entity = await unitOfWork.Repository<InventoryCategory>()
                                     .Query(asNoTracking: false)
                                     .IgnoreQueryFilters()
                                     .SingleOrDefaultAsync(x => x.Id == categoryId && x.IsDeleted, cancellationToken);

        if (entity is null) return InventoryErrors.CategoryNotFound;
        if (await Duplicate(entity.Name, entity.Id, cancellationToken, includeDeleted: false)) return InventoryErrors.DuplicateCategoryName;

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedByUserId = null;
        await Audit("InventoryCategory.Restored", entity.Id, "محذوف", Values(entity), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(InventoryErrors.CategoryRestored.Description);
    }
    private async Task<bool> ValidateParent(Guid? parentId, Guid? childId, CancellationToken ct)
    {
        if (parentId is null) return true;
        if (parentId == childId) return false;

        var all = await unitOfWork.Repository<InventoryCategory>().Query().Select(x => new { x.Id, x.ParentCategoryId }).ToListAsync(ct);
        var map = all.ToDictionary(x => x.Id, x => x.ParentCategoryId);
        if (!map.ContainsKey(parentId.Value)) return false;
        var cursor = map[parentId.Value]; var guard = 0;
        while (cursor is not null)
        {
            if (cursor == childId) return false;
            if (++guard > 100) return false;
            cursor = map.TryGetValue(cursor.Value, out var next) ? next : null;
        }
        return true;
    }

    private bool Allowed(string code) => PermissionGuard.Check(permissions, code, InventoryErrors.PermissionDenied) is null;

    private async Task<bool> Duplicate(string name, Guid? excludeId, CancellationToken ct, bool includeDeleted = false)
    {
        var q = unitOfWork.Repository<InventoryCategory>().Query();

        if (includeDeleted) q = q.IgnoreQueryFilters();

        var rows = await q.Where(x => excludeId == null || x.Id != excludeId)
            .Select(x => x.Name).ToListAsync(ct);

        var key = ArabicNameNormalizer.Normalize(name);
        return rows.Any(x => ArabicNameNormalizer.Normalize(x) == key);
    }
    private async Task Audit(string action, Guid id, string before, string after, CancellationToken ct) => await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog { Action = action, EntityType = nameof(InventoryCategory), EntityId = id.ToString("D"), UserAccountId = sessions.CurrentUser?.UserId, ChangeSummary = $"قبل: {before}; بعد: {after}; المستخدم: {sessions.CurrentUser?.FullName ?? "—"}" }, ct);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Values(InventoryCategory x) => $"الاسم: {x.Name}; الوصف: {x.Description ?? "—"}; الحالة: {(x.IsActive ? "نشط" : "موقوف")}";
    private static InventoryCategoryDto ToDto(InventoryCategory x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Description = x.Description,
        IsActive = x.IsActive,
        ItemsCount = x.Items.Count(i => !i.IsDeleted),
        ParentCategoryId = x.ParentCategoryId,
        ParentCategoryName = x.ParentCategory?.Name
    };
}
