using ElMomenNursery.Application.DTOs;

namespace ElMomenNursery.Application.IServices;

public interface IInventoryCategoryService
{
    Task<InventoryCategoryDto> CreateCategoryAsync(CreateCategoryDto dto);
    Task<InventoryCategoryDto> GetCategoryByIdAsync(Guid categoryId);
    Task<IEnumerable<InventoryCategoryDto>> GetAllCategoriesAsync();
    Task<InventoryCategoryDto> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto dto);
    Task DeleteCategoryAsync(Guid categoryId);
}
