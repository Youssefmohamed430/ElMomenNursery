namespace ElMomenNursery.Domain.Entities;

public sealed class AuthenticationAuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public required string Action { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}
