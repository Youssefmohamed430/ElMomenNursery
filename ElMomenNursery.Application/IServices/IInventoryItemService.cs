using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IInventoryItemService
{
    Task<Result<IReadOnlyList<InventoryItemDto>>> GetItemsAsync(InventoryItemFilter filter, CancellationToken cancellationToken = default);
    Task<Result<InventoryItemDto>> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<Result<InventoryItemDto>> CreateItemAsync(CreateItemDto dto, CancellationToken cancellationToken = default);
    Task<Result<InventoryItemDto>> UpdateItemAsync(Guid itemId, UpdateItemDto dto, CancellationToken cancellationToken = default);
    Task<Result> SetItemActiveAsync(Guid itemId, bool isActive, CancellationToken cancellationToken = default);
    Task<Result> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InventoryItemDto>>> GetDeletedItemsAsync(CancellationToken cancellationToken = default);
    Task<Result> RestoreItemAsync(Guid itemId, CancellationToken cancellationToken = default);
}
