namespace ElMomenNursery.Application.IServices;

public interface IPermissionService
{
    bool HasPermission(string code);
    void EnsurePermission(string code);
    bool HasAnyPermission(params string[] codes);
    bool HasAllPermissions(params string[] codes);
    Task RefreshCurrentUserAsync(CancellationToken cancellationToken = default);
}
