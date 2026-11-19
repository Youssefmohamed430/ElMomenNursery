namespace ElMomenNursery.Domain.Entities;

public sealed class InventoryItem : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public required string Name { get; set; }
    public Guid CategoryId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; } = true;
    public required InventoryCategory Category { get; set; }
    public ICollection<StockMovement> StockMovements { get; } = new List<StockMovement>();
    public ICollection<InvoiceLine> InvoiceLines { get; } = new List<InvoiceLine>();
}
