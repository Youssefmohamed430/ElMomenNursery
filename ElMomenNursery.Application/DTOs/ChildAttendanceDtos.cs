using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public sealed record ChildAttendanceDto(Guid Id, Guid ChildId, DateOnly Date, AttendanceStatus Status,
    DateTimeOffset? CheckIn, DateTimeOffset? CheckOut, AttendanceEntryMethod EntryMethod, string? Notes,
    string? Alert = null);
public sealed record CreateChildAttendanceDto(Guid ChildId, DateOnly Date, AttendanceStatus Status,
    DateTimeOffset? CheckIn = null, DateTimeOffset? CheckOut = null, string? Notes = null);
public sealed record UpdateChildAttendanceDto(AttendanceStatus Status, DateTimeOffset? CheckIn = null,
    DateTimeOffset? CheckOut = null, string? Notes = null, string? Reason = null);
public sealed record ChildDailyAttendanceQuery(DateOnly Date, Guid? ClassroomId = null);
public sealed record AttendanceChildOptionDto(Guid Id, string FullName);
