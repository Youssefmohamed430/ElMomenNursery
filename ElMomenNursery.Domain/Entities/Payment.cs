namespace ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;

public sealed class Payment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset PaidAt { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Posted;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public required Child Child { get; set; }
    public Subscription? Subscription { get; set; }
    public UserAccount? ReceivedByUser { get; set; }
    public ICollection<PaymentAllocation> Allocations { get; } = new List<PaymentAllocation>();
}
