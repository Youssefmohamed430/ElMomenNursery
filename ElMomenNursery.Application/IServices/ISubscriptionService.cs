using ElMomenNursery.Application.DTOs;

namespace ElMomenNursery.Application.IServices;

public interface ISubscriptionService
{
    Task<List<SubscriptionDto>> GetAllSubscriptions();
    Task<List<SubscriptionDto>> GetAllSubscriptionsForChild(Guid childId);
    Task<List<SubscriptionDto>> GetAllSubscriptionsForChildByRangeOfDate(Guid ChildId, DateOnly Startdate, DateOnly EndDate);
    Task<List<SubscriptionDto>> GetAllSubscriptionsByRangeOfDate(DateOnly Startdate, DateOnly EndDate);
    Task<SubscriptionDto> AddSubscription(CreateSubscriptionDto createSubscription);
    Task<SubscriptionDto> UpdateSubscription(UpdateSubscriptionDto updateSubscription, Guid ChildId);
    Task<bool> DeleteSubscription(Guid subscriptionId);
}
