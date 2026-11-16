using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface ISubscriptionService
{
    Task<Result<IReadOnlyList<SubscriptionDto>>> GetSubscriptionsAsync(SubscriptionFilter filter, CancellationToken cancellationToken = default);
    Task<Result<SubscriptionDto>> GetSubscriptionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SubscriptionDto>>> GetSubscriptionsForChildAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ChildLookupDto>>> GetChildLookupAsync(string? search, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SubscriptionPlanOptionDto>>> GetPlansAsync(CancellationToken cancellationToken = default);
    Task<Result<SubscriptionDto>> CreateSubscriptionAsync(CreateSubscriptionDto dto, CancellationToken cancellationToken = default);
    Task<Result<SubscriptionDto>> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionDto dto, CancellationToken cancellationToken = default);
    Task<Result> CancelSubscriptionAsync(Guid id, string reason, CancellationToken cancellationToken = default);
    Task<Result> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PaymentReceiptDto>> RecordPaymentAsync(Guid subscriptionId, RecordPaymentDto dto, CancellationToken cancellationToken = default);
    Task<Result<PaymentReceiptDto>> GetReceiptAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<PaymentReceiptDto>>> GetPaymentsForSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);
}
