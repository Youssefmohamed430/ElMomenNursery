using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IInventoryItemService
{
    Result<Task<InventoryItemDto>> GetItemByIdAsync(Guid itemId);
    Result<Task<IEnumerable<InventoryItemDto>>> GetAllItemsAsync();
    Result<Task<IEnumerable<InventoryItemDto>>> GetItemsByCategoryIdAsync(Guid categoryId);
    Result<Task<InventoryItemDto>> CreateItemAsync(CreateItemDto dto);
    Result<Task<InventoryItemDto>> UpdateItemAsync(Guid itemId, UpdateItemDto dto);
    Result<Task> DeleteItemAsync(Guid itemId);
}
