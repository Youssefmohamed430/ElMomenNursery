namespace ElMomenNursery.Domain.Entities;

public sealed class SubscriptionPlan
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public decimal DefaultFee { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public ICollection<Subscription> Subscriptions { get; } = new List<Subscription>();
}
