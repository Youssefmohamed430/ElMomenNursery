namespace ElMomenNursery.Application.DTOs;

public sealed class InventoryItemDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public required int Quantity { get; init; }
    public bool IsActive { get; init; }
    public bool CategoryIsActive { get; init; }
}
