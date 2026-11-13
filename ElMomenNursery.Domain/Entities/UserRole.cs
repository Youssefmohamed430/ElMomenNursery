namespace ElMomenNursery.Domain.Entities;

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public UserAccount User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
