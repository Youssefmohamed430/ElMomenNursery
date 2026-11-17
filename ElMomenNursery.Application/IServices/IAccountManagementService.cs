using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public sealed record AccountSummary(Guid Id, string Username, string FullName, bool IsActive, IReadOnlyList<string> Roles);
public sealed record RoleSummary(Guid Id, string Name, IReadOnlyList<string> PermissionCodes);

public interface IAccountManagementService
{
    Task<Result<IReadOnlyList<AccountSummary>>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RoleSummary>>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<Result> CreateAccountAsync(string username, string fullName, string password, Guid roleId, CancellationToken cancellationToken = default);
    Task<Result> SetAccountActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);
    Task<Result> SetUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);
    Task<Result> SetRolePermissionsAsync(Guid roleId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken = default);
}
