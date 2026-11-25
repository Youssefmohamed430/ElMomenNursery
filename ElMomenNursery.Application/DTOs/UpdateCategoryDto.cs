namespace ElMomenNursery.Application.DTOs;

public sealed class UpdateCategoryDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
}
