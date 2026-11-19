using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IInventoryCategoryService
{
    Task<Result<InventoryCategoryDto>> CreateCategoryAsync(CreateCategoryDto dto);
    Task<Result<InventoryCategoryDto>> GetCategoryByIdAsync(Guid categoryId);
    Task<Result<IEnumerable<InventoryCategoryDto>>> GetAllCategoriesAsync();
    Task<Result<InventoryCategoryDto>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto dto);
    Task<Result> DeleteCategoryAsync(Guid categoryId);
}
