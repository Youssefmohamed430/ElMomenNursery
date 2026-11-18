namespace ElMomenNursery.Application.DTOs;

public class CreateItemDto
{
    public required string Name { get; set; }
    public Guid CategoryId { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
}
