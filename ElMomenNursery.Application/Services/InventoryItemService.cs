using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;

namespace ElMomenNursery.Application.Services;

public class InventoryItemService(IUnitOfWork unitOfWork) : IInventoryItemService
{
    public async Task<Result<InventoryItemDto>> CreateItemAsync(CreateItemDto dto)
    {
        var categoryExists = await unitOfWork.Repository<InventoryCategory>().AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
            return Result.Failure<InventoryItemDto>(new Error("CategoryNotFound", "هذه الفئة غير موجودة."));

        var newItem = dto.Adapt<InventoryItem>();
        await unitOfWork.Repository<InventoryItem>().AddAsync(newItem);
        await unitOfWork.SaveChangesAsync();

        return Result.Success(newItem.Adapt<InventoryItemDto>());
    }

    public async Task<Result> DeleteItemAsync(Guid itemId)
    {
        var item = await unitOfWork.Repository<InventoryItem>().FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null)
            return Result.Failure(new Error("ItemNotFound", "العنصر غير موجود."));

        unitOfWork.Repository<InventoryItem>().Remove(item);
        await unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<IEnumerable<InventoryItemDto>>> GetAllItemsAsync()
    {
        var items = await unitOfWork.Repository<InventoryItem>().ListAsync();
        return Result.Success(items.Adapt<IEnumerable<InventoryItemDto>>());
    }

    public async Task<Result<InventoryItemDto>> GetItemByIdAsync(Guid itemId)
    {
        var item = await unitOfWork.Repository<InventoryItem>().FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null)
            return Result.Failure<InventoryItemDto>(new Error("ItemNotFound", "العنصر غير موجود."));

        return Result.Success(item.Adapt<InventoryItemDto>());  
    }

    public async Task<Result<IEnumerable<InventoryItemDto>>> GetItemsByCategoryIdAsync(Guid categoryId)
    {
        var items = await unitOfWork.Repository<InventoryItem>().ListAsync(i => i.CategoryId == categoryId);
        return Result.Success(items.Adapt<IEnumerable<InventoryItemDto>>());
    }

    public async Task<Result<InventoryItemDto>> UpdateItemAsync(Guid itemId, UpdateItemDto dto)
    {
        var item = await unitOfWork.Repository<InventoryItem>().FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null)
            return Result.Failure<InventoryItemDto>(new Error("ItemNotFound", "العنصر غير موجود."));

        item.Adapt(dto);
        await unitOfWork.SaveChangesAsync();

        return Result.Success(item.Adapt<InventoryItemDto>());
    }
}
