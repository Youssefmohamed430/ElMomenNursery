namespace ElMomenNursery.Domain.Entities;

public sealed class InventoryCategory : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public InventoryCategory? ParentCategory { get; set; }
    public ICollection<InventoryCategory> Children { get; } = new List<InventoryCategory>();
    public bool IsActive { get; set; } = true;
    public ICollection<InventoryItem> Items { get; } = new List<InventoryItem>();
}
