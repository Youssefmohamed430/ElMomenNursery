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
    public decimal? AmountDue { get; set; }
    public string? Notes { get; set; }
    public InitialPaymentDto? InitialPayment { get; set; }
    public bool ConfirmOverlappingPeriod { get; set; }
}
