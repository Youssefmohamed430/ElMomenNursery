namespace ElMomenNursery.Application.DTOs;

public sealed record LookupItemDto(Guid Id, string Category, string Name, int SortOrder, bool IsActive, bool IsSystem);
public sealed record SaveLookupItemDto(string Category, string Name, int SortOrder, bool IsActive);
