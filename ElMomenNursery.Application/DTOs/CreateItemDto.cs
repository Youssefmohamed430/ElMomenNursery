namespace ElMomenNursery.Application.DTOs;

public sealed class CreateItemDto
{
    public required string Name { get; init; }
    public Guid CategoryId { get; init; }
    public decimal UnitPrice { get; init; }
    public required int Quantity { get; init; }
    public bool IsActive { get; init; }
}
