namespace ElMomenNursery.Domain.Entities;

public sealed class AttendanceDevice : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public required string DeviceType { get; set; }
    public required string DisplayName { get; set; }
    public string? ConnectionDetails { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public ICollection<DeviceAttendanceEvent> Events { get; } = new List<DeviceAttendanceEvent>();
}
