namespace ElMomenNursery.Application.DTOs;

public class UpdateSubscriptionDto
{
    public Guid? SubscriptionPlanId { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public decimal AmountDue { get; init; }
    public string? Notes { get; init; }
    public required string Reason { get; init; }
}
