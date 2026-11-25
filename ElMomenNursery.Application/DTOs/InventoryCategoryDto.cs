namespace ElMomenNursery.Application.DTOs;

public sealed class InventoryCategoryDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int ItemsCount { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}
