using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public sealed class EmployeeDailyAttendanceDto
{
    public Guid EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public required string JobDesignation { get; init; }
    public Guid? AttendanceId { get; init; }
    public DateOnly AttendanceDate { get; init; }
    public AttendanceStatus? Status { get; init; }
    public DateTimeOffset? CheckInTime { get; init; }
    public DateTimeOffset? CheckOutTime { get; init; }
    public AttendanceEntryMethod? EntryMethod { get; init; }
    public string? Notes { get; init; }
    public Guid? RecordedByUserId { get; init; }
    public bool IsMarked => AttendanceId.HasValue;
}
