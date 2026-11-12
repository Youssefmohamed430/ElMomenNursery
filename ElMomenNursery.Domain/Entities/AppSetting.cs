namespace ElMomenNursery.Domain.Entities;

public sealed class AppSetting
{
    public required string Key { get; init; }
    public required string Value { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
