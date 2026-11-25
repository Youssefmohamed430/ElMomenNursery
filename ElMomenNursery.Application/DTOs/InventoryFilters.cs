namespace ElMomenNursery.Application.DTOs;

public sealed record InventoryItemFilter(string? SearchText = null, Guid? CategoryId = null, bool? IsActive = null, decimal? MinPrice = null, decimal? MaxPrice = null);
public sealed record InventoryCategoryFilter(string? SearchText = null, bool? IsActive = null);
public sealed record InventoryCategoryLookupDto(Guid Id, string Name, bool IsActive, Guid? ParentCategoryId = null)
{
    public override string ToString() => Name;
}
