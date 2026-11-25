namespace ElMomenNursery.Application.DTOs;
public sealed record SubscriptionPlanDto(Guid Id, string Name, decimal DefaultFee, string? Description, bool IsActive);
public sealed record SaveSubscriptionPlanDto(string Name, decimal DefaultFee, string? Description);
