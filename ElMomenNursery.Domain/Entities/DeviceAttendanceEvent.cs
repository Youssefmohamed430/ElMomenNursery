namespace ElMomenNursery.Domain.Entities;

public sealed class DeviceAttendanceEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string DeviceEventIdentifier { get; set; }
    public Guid AttendanceDeviceId { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public Guid? ChildId { get; set; }
    public Guid? EmployeeId { get; set; }
    public bool IsProcessed { get; set; }
    public Guid? LinkedAttendanceId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public required AttendanceDevice AttendanceDevice { get; set; }
    public Child? Child { get; set; }
    public Employee? Employee { get; set; }
}
