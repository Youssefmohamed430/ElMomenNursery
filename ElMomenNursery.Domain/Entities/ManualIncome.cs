namespace ElMomenNursery.Domain.Entities;

public sealed class ManualIncome : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public DateOnly IncomeDate { get; set; }
    public required string Source { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    public string? PayerName { get; set; }
    public string? Reference { get; set; }
    public string? IdempotencyKey { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public UserAccount? RecordedByUser { get; set; }
}

