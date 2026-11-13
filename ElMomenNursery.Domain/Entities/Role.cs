namespace ElMomenNursery.Domain.Entities;

public sealed class Role
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public ICollection<UserAccount> UserAccounts { get; } = new List<UserAccount>();
    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();
}
