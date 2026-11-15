using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public class UpdateEmployeeAttendanceDto
{
    public DateTimeOffset? CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}
