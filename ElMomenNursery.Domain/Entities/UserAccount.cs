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
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public Employee? Employee { get; set; }
    public required Role Role { get; set; }
    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();
}
