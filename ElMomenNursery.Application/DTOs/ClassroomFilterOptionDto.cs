namespace ElMomenNursery.Application.DTOs;

public sealed record ClassroomFilterOptionDto(Guid Id, string Name)
{
    public override string ToString() => Name;
}
