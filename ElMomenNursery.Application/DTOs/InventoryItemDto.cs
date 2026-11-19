namespace ElMomenNursery.Application.DTOs;

public class InventoryItemDto
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
}
