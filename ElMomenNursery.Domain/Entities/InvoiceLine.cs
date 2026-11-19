namespace ElMomenNursery.Domain.Entities;

public sealed class InvoiceLine
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public required string ItemNameSnapshot { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public required Invoice Invoice { get; set; }
    public InventoryItem? InventoryItem { get; set; }
}
