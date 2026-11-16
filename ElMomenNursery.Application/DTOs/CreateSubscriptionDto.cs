using ElMomenNursery.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElMomenNursery.Application.DTOs;

public class CreateSubscriptionDto
{
    [Required(ErrorMessage = "ChildId is required.")]
    public required Guid ChildId { get; set; }
    public Guid? SubscriptionPlanId { get; set; }
    [Required(ErrorMessage = "PeriodStart is required.")]
    public required DateOnly PeriodStart { get; set; }
    [Required(ErrorMessage = "PeriodEnd is required.")]
    public required DateOnly PeriodEnd { get; set; }
    [Required(ErrorMessage = "AmountDue is required.")]
    public required decimal AmountDue { get; set; }
    [Required(ErrorMessage = "Status is required.")]
    public required SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
    public string? Notes { get; set; }
}
