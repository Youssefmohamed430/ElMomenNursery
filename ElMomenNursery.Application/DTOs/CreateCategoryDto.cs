namespace ElMomenNursery.Application.DTOs;

public sealed class CreateCategoryDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public Guid? ParentCategoryId { get; init; }
}
