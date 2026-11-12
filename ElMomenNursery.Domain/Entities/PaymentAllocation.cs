namespace ElMomenNursery.Domain.Entities;

public sealed class PaymentAllocation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PaymentId { get; set; }
    public Guid SubscriptionId { get; set; }
    public decimal AmountApplied { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public required Payment Payment { get; set; }
    public required Subscription Subscription { get; set; }
}
