namespace ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;

public sealed class Subscription : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public Guid ChildId { get; set; }
    public Guid? SubscriptionPlanId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal AmountDue { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public required Child Child { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }
    public ICollection<PaymentAllocation> PaymentAllocations { get; } = new List<PaymentAllocation>();
}
