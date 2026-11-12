namespace ElMomenNursery.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public Guid? UserAccountId { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? ChangeSummary { get; set; }
    public UserAccount? UserAccount { get; set; }
}
