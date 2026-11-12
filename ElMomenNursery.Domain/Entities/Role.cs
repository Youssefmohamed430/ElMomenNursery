namespace ElMomenNursery.Domain.Entities;

public sealed class Role
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<UserAccount> UserAccounts { get; } = new List<UserAccount>();
}
