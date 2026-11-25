using System.ComponentModel.DataAnnotations;
using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public class CreateSubscriptionDto
{
    [Required(ErrorMessage = "يجب اختيار الطفل.")]
    public required Guid ChildId { get; set; }
    public Guid? SubscriptionPlanId { get; set; }
    [Required(ErrorMessage = "تاريخ بداية الاشتراك مطلوب.")]
    public required DateOnly PeriodStart { get; set; }
    [Required(ErrorMessage = "تاريخ نهاية الاشتراك مطلوب.")]
    public required DateOnly PeriodEnd { get; set; }
    public decimal? AmountDue { get; set; }
    public string? Notes { get; set; }
    public InitialPaymentDto? InitialPayment { get; set; }
    public bool ConfirmOverlappingPeriod { get; set; }
    public SubscriptionStatus? StatusOverride { get; set; }
}
