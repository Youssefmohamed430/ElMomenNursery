using ElMomenNursery.Domain.Entities;

namespace ElMomenNursery.Application.DTOs;

public class InventoryCategoryDto
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
