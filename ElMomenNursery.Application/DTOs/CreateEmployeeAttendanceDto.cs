using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public class CreateEmployeeAttendanceDto
{
    public Guid EmployeeId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public DateTimeOffset? CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Notes { get; set; }
}
