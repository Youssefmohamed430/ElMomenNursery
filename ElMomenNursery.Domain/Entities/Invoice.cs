namespace ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;

public sealed class Invoice
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string InvoiceNumber { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public Guid? ChildId { get; set; }
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public Guid? CreatedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Child? Child { get; set; }
    public UserAccount? CreatedByUser { get; set; }
    public ICollection<InvoiceLine> Lines { get; } = new List<InvoiceLine>();
}
