namespace ElMomenNursery.Application.DTOs;

public sealed record ClassroomDto(Guid Id, string Name, Guid? TeacherId, string? TeacherName, int ChildCount);
public sealed record ClassroomChildDto(Guid Id, string Name, bool IsAssigned);
public sealed record ClassroomTeacherDto(Guid Id, string Name);
