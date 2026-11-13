namespace ElMomenNursery.Application.IServices;

public sealed class UserSession(Guid userId, string username, string fullName, Guid? employeeId,
    IReadOnlySet<string> roles, IReadOnlySet<string> permissions, DateTimeOffset? loginTime = null)
{
    public Guid UserId { get; } = userId;
    public string Username { get; private set; } = username;
    public string FullName { get; private set; } = fullName;
    public Guid? EmployeeId { get; private set; } = employeeId;
    public IReadOnlySet<string> Roles { get; private set; } = roles;
    public IReadOnlySet<string> Permissions { get; private set; } = permissions;
    public DateTimeOffset LoginTime { get; } = loginTime ?? DateTimeOffset.UtcNow;
    public bool HasPermission(string code) => Permissions.Contains(code);
    public bool IsInRole(string role) => Roles.Contains(role);

    public void RefreshAuthorization(string username, string fullName, Guid? employeeId,
        IReadOnlySet<string> roles, IReadOnlySet<string> permissions)
    {
        Username = username; FullName = fullName; EmployeeId = employeeId; Roles = roles; Permissions = permissions;
    }
}
