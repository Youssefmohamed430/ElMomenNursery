namespace ElMomenNursery.Domain.Entities;

public sealed class Expense
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateOnly ExpenseDate { get; set; }
    public required string Category { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    public string? PaymentReference { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public bool IsVoided { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public UserAccount? RecordedByUser { get; set; }
}
