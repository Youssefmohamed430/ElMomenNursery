namespace ElMomenNursery.Application.DTOs;

public sealed class UpdateItemDto
{
    public required string Name { get; init; }
    public Guid CategoryId { get; init; }
    public decimal UnitPrice { get; init; }
    public required int Quantity { get; init; }
    public int? ExpectedQuantity { get; init; }
    public string? QuantityAdjustmentReason { get; init; }
    public bool IsActive { get; init; }
}
