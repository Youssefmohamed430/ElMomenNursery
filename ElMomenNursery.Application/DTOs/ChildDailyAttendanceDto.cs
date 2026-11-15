using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public sealed class ChildDailyAttendanceDto
{
    public Guid ChildId { get; init; }
    public required string ChildName { get; init; }
    public required string ClassroomNames { get; init; }
    public Guid? AttendanceId { get; init; }
    public DateOnly Date { get; init; }
    public AttendanceStatus? Status { get; init; }
    public DateTimeOffset? CheckIn { get; init; }
    public DateTimeOffset? CheckOut { get; init; }
    public AttendanceEntryMethod? EntryMethod { get; init; }
    public string? Notes { get; init; }
    public bool IsMarked => AttendanceId.HasValue;
}
