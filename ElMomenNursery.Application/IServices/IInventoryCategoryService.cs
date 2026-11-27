using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IInventoryCategoryService
{
    Task<Result<IReadOnlyList<InventoryCategoryDto>>> GetCategoriesAsync(InventoryCategoryFilter filter, CancellationToken cancellationToken = default);
    Task<Result<InventoryCategoryDto>> GetCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InventoryCategoryLookupDto>>> GetCategoryLookupAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InventoryCategoryNodeDto>>> GetCategoryTreeAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<Result<InventoryCategoryDto>> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<Result<InventoryCategoryDto>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<Result> SetCategoryActiveAsync(Guid categoryId, bool isActive, CancellationToken cancellationToken = default);
    Task<Result> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InventoryCategoryDto>>> GetDeletedCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Result> RestoreCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
