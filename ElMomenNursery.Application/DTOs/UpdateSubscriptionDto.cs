namespace ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Enums;

public class UpdateSubscriptionDto
{
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public decimal AmountDue { get; init; }
    public string? Notes { get; init; }
    public required string Reason { get; init; }
    public SubscriptionStatus? StatusOverride { get; init; }
}
