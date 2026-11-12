namespace ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;

public sealed class StockMovement
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public StockMovementType MovementType { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public decimal? UnitCost { get; set; }
    public Guid? InvoiceLineId { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public required InventoryItem InventoryItem { get; set; }
    public InvoiceLine? InvoiceLine { get; set; }
    public UserAccount? RecordedByUser { get; set; }
}
