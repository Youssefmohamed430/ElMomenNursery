namespace ElMomenNursery.Application.DTOs;

public sealed record InventoryCategoryNodeDto(
    Guid Id, string Name, bool IsActive, int Depth,
    IReadOnlyList<InventoryCategoryNodeDto> Children,
    IReadOnlyList<InventoryItemDto>? Items = null);
