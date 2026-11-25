using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface ILookupService
{
    Task<Result<IReadOnlyList<LookupItemDto>>> GetAsync(string category, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<Result<LookupItemDto>> CreateAsync(SaveLookupItemDto dto, CancellationToken cancellationToken = default);
    Task<Result<LookupItemDto>> UpdateAsync(Guid id, SaveLookupItemDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
