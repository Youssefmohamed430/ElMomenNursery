using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public sealed record SubscriptionFilter
{
    public string? SearchText { get; init; }
    public SubscriptionStatus? Status { get; init; }
    public Guid? PlanId { get; init; }
    public Guid? ClassroomId { get; init; }
    public DateOnly? PeriodFrom { get; init; }
    public DateOnly? PeriodTo { get; init; }
    public bool OnlyWithBalance { get; init; }
}
