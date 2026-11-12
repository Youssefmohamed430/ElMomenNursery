namespace ElMomenNursery.Domain.Entities;

public sealed class AttendanceDevice
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string DeviceType { get; set; }
    public required string DisplayName { get; set; }
    public string? ConnectionDetails { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public ICollection<DeviceAttendanceEvent> Events { get; } = new List<DeviceAttendanceEvent>();
}
