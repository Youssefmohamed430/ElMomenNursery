using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;

namespace ElMomenNursery.Application.Services;

public class InventoryCategoryService(IUnitOfWork unitOfWork) : IInventoryCategoryService
{
    public async Task<Result<InventoryCategoryDto>> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var newCategory = dto.Adapt<InventoryCategory>();

        await unitOfWork.Repository<InventoryCategory>().AddAsync(newCategory);
        await unitOfWork.SaveChangesAsync();

        return Result.Success(newCategory.Adapt<InventoryCategoryDto>());
    }

    public async Task<Result> DeleteCategoryAsync(Guid categoryId)
    {
        var category = await unitOfWork.Repository<InventoryCategory>().FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null) return Result.Failure(new Error("CategoryNotFound", "هذه الفئة غير موجودة"));

        unitOfWork.Repository<InventoryCategory>().Remove(category);
        await unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<IEnumerable<InventoryCategoryDto>>> GetAllCategoriesAsync()
    {
        var categories = await unitOfWork.Repository<InventoryCategory>().ListAsync();
        return Result.Success(categories.Adapt<IEnumerable<InventoryCategoryDto>>());
    }

    public async Task<Result<InventoryCategoryDto>> GetCategoryByIdAsync(Guid categoryId)
    {
        var category = await unitOfWork.Repository<InventoryCategory>().FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null) return Result.Failure<InventoryCategoryDto>(new Error("CategoryNotFound", "هذه الفئة غير موجودة"));

        return Result.Success(category.Adapt<InventoryCategoryDto>());
    }

    public async Task<Result<InventoryCategoryDto>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto dto)
    {
        var category = await unitOfWork.Repository<InventoryCategory>().FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null) return Result.Failure<InventoryCategoryDto>(new Error("CategoryNotFound", "هذه الفئة غير موجودة"));

        category.Name = dto.Name;
        category.Description = dto.Description;

        await unitOfWork.SaveChangesAsync();

        return Result.Success(category.Adapt<InventoryCategoryDto>());
    }
}
