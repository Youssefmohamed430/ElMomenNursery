using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;
namespace ElMomenNursery.Application.IServices;
public interface ISubscriptionPlanService
{
    Task<Result<IReadOnlyList<SubscriptionPlanDto>>> GetAsync(CancellationToken ct = default);
    Task<Result<SubscriptionPlanDto>> CreateAsync(SaveSubscriptionPlanDto dto, CancellationToken ct = default);
    Task<Result<SubscriptionPlanDto>> UpdateAsync(Guid id, SaveSubscriptionPlanDto dto, CancellationToken ct = default);
    Task<Result> SetActiveAsync(Guid id, bool active, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
