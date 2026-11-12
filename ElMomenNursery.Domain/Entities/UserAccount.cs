namespace ElMomenNursery.Domain.Entities;

public sealed class UserAccount
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? EmployeeId { get; set; }
    public required string Username { get; set; }
    public string UsernameNormalized { get; set; } = string.Empty;
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid RoleId { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public Employee? Employee { get; set; }
    public required Role Role { get; set; }
}
