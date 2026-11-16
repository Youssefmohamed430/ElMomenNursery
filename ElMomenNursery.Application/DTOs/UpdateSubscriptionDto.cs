using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public class UpdateSubscriptionDto
{
    public Guid? SubscriptionPlanId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal AmountDue { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
    public string? Notes { get; set; }
}
