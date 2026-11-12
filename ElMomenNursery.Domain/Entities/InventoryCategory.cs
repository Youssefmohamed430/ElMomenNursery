namespace ElMomenNursery.Domain.Entities;

public sealed class InventoryCategory
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<InventoryItem> Items { get; } = new List<InventoryItem>();
}
