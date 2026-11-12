namespace ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;

public sealed class EmployeeAttendance
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public DateTimeOffset? CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public AttendanceStatus Status { get; set; }
    public AttendanceEntryMethod EntryMethod { get; set; } = AttendanceEntryMethod.Manual;
    public Guid? RecordedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public required Employee Employee { get; set; }
    public UserAccount? RecordedByUser { get; set; }
}
