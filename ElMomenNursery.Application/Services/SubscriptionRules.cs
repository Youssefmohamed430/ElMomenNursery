namespace ElMomenNursery.Application.Services;

public static class SubscriptionRules
{
    public static Error? ValidatePeriod(DateOnly start, DateOnly end) => start == default || end < start ? SubscriptionErrors.InvalidPeriod : null;
    public static Error? ValidateAmount(decimal amount) => amount < 0 ? SubscriptionErrors.InvalidAmount : null;
    public static Error? ValidatePayment(decimal amount, decimal remaining) => amount <= 0 ? SubscriptionErrors.InvalidAmount
        : amount > remaining ? SubscriptionErrors.OverpaymentNotAllowed : null;
    public static Error? ValidateAmountUpdate(decimal amount, decimal paid) => amount < 0 ? SubscriptionErrors.InvalidAmount
        : amount < paid ? SubscriptionErrors.AmountBelowPaid : null;
    public static SubscriptionStatus ComputeStatus(decimal due, decimal paid) => due > 0 && paid >= due ? SubscriptionStatus.Paid
        : paid > 0 && paid < due ? SubscriptionStatus.PartiallyPaid : SubscriptionStatus.Pending;
}
