using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public class EmployeeAttendanceDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly AttendanceDate { get; set; }
    public DateTimeOffset? CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public AttendanceStatus Status { get; set; }
    public AttendanceEntryMethod EntryMethod { get; set; } = AttendanceEntryMethod.Manual;
    public Guid? RecordedByUserId { get; set; }
    public string? Notes { get; set; }
}
