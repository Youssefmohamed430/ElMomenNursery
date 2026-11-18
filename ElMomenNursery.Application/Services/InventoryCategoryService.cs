using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;

namespace ElMomenNursery.Application.Services;

public class InventoryCategoryService : IInventoryCategoryService
{
    public Task<InventoryCategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        throw new NotImplementedException();
    }

    public Task DeleteCategoryAsync(Guid categoryId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<InventoryCategoryDto>> GetAllCategoriesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<InventoryCategoryDto> GetCategoryByIdAsync(Guid categoryId)
    {
        throw new NotImplementedException();
    }

    public Task<InventoryCategoryDto> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto dto)
    {
        throw new NotImplementedException();
    }
}
