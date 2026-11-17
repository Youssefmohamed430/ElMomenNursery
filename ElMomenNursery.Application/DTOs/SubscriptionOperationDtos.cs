using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public sealed record ChildLookupDto(Guid Id, string Name) { public override string ToString() => Name; }
public sealed record SubscriptionPlanOptionDto(Guid Id, string Name, decimal DefaultFee);
public sealed record InitialPaymentDto(decimal Amount, PaymentMethod PaymentMethod, string? Reference = null);
public sealed record RecordPaymentDto(decimal Amount, PaymentMethod PaymentMethod, string? Reference = null, string? Notes = null, string? Reason = null);
public sealed record PaymentReceiptDto(Guid PaymentId, string ReceiptNumber, DateTimeOffset PaidAt, string ChildName,
    string SubscriptionPlanName, DateOnly PeriodStart, DateOnly PeriodEnd, decimal AmountDue, decimal PaidBefore,
    decimal PaidNow, decimal RemainingAmount, PaymentMethod PaymentMethod, string? Reference, string? ReceivedBy,
    string? Notes, string NurseryName = "حضانة المؤمن", string? NurseryAddress = null, string? NurseryPhone = null);
