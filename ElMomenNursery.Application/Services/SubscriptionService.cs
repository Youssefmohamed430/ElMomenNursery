using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace ElMomenNursery.Application.Services;

public class SubscriptionService(IUnitOfWork unitOfWork) : ISubscriptionService
{
    public async Task<SubscriptionDto> AddSubscription(CreateSubscriptionDto createSubscription)
    {
        var NewSubscription = createSubscription.Adapt<Subscription>();

        await unitOfWork.Repository<Subscription>().AddAsync(NewSubscription);
        await unitOfWork.SaveChangesAsync();

        return NewSubscription.Adapt<SubscriptionDto>();
    }

    public async Task<bool> DeleteSubscription(Guid subscriptionId)
    {
        var subscription = await unitOfWork.Repository<Subscription>().FirstOrDefaultAsync(s => s.Id == subscriptionId);
        if (subscription == null) return false;

        unitOfWork.Repository<Subscription>().Remove(subscription);
        await unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<List<SubscriptionDto>> GetAllSubscriptions()
    {
        var subscriptions = await unitOfWork.Repository<Subscription>().ListAsync();
        return subscriptions.Adapt<List<SubscriptionDto>>();
    }

    public async Task<List<SubscriptionDto>> GetAllSubscriptionsByRangeOfDate(DateOnly Startdate, DateOnly EndDate)
    {
        var subscriptions = await unitOfWork.Repository<Subscription>().ListAsync(s => s.PeriodStart <= Startdate && s.PeriodEnd >= EndDate);
        return subscriptions.Adapt<List<SubscriptionDto>>();
    }

    public async Task<List<SubscriptionDto>> GetAllSubscriptionsForChild(Guid childId)
    {
        var subscriptions = await unitOfWork.Repository<Subscription>().ListAsync(s => s.ChildId == childId);
        return subscriptions.Adapt<List<SubscriptionDto>>();
    }

    public async Task<List<SubscriptionDto>> GetAllSubscriptionsForChildByRangeOfDate(Guid ChildId, DateOnly Startdate, DateOnly EndDate)
    {
        var subscriptions = await  unitOfWork.Repository<Subscription>().ListAsync(s => s.ChildId == ChildId && s.PeriodStart <= Startdate && s.PeriodEnd >= EndDate);
        return subscriptions.Adapt<List<SubscriptionDto>>();
    }

    public async Task<SubscriptionDto> UpdateSubscription(UpdateSubscriptionDto updateSubscription, Guid ChildId)
    {
        var subscription = await unitOfWork.Repository<Subscription>().FirstOrDefaultAsync(s => s.ChildId == ChildId);

        subscription.SubscriptionPlanId = updateSubscription.SubscriptionPlanId ?? subscription.SubscriptionPlanId;
        subscription.PeriodStart = updateSubscription.PeriodStart != default ? updateSubscription.PeriodStart : subscription.PeriodStart;
        subscription.PeriodEnd = updateSubscription.PeriodEnd != default ? updateSubscription.PeriodEnd : subscription.PeriodEnd;
        subscription.AmountDue = updateSubscription.AmountDue != default ? updateSubscription.AmountDue : subscription.AmountDue;
        subscription.Status = updateSubscription.Status != default ? updateSubscription.Status : subscription.Status;
        subscription.Notes = updateSubscription.Notes ?? subscription.Notes;

        await unitOfWork.SaveChangesAsync();

        return subscription.Adapt<SubscriptionDto>();
    }
}
