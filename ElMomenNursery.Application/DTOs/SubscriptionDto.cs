using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public class SubscriptionDto
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public string ChildName { get; set; }
    public Guid? SubscriptionPlanId { get; set; }
    public string? SubscriptionPlanName { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal AmountDue { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
