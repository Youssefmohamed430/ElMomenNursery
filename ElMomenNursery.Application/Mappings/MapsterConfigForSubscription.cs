using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Entities;

namespace ElMomenNursery.Application.Mappings;

public static class MapsterConfigForSubscription
{
    public static readonly TypeAdapterConfig Config = CreateConfig();
    private static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<Subscription, SubscriptionDto>()
            .Map(x => x.ChildName, x => x.Child.FullName);
        config.NewConfig<CreateSubscriptionDto, Subscription>()
            .Ignore(nameof(Subscription.Child))
            .Ignore(nameof(Subscription.PaymentAllocations)).Ignore(nameof(Subscription.Status));
        return config;
    }
}
