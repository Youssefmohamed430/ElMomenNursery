namespace ElMomenNursery.Domain.Entities;

public sealed class SubscriptionPlan : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public required string Name { get; set; }
    public decimal DefaultFee { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public ICollection<Subscription> Subscriptions { get; } = new List<Subscription>();
}
