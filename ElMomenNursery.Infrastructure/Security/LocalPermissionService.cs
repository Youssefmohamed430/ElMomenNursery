using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Mappings;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Mapster;

namespace ElMomenNursery.Infrastructure.Security;

public sealed class LocalPermissionService(NurseryDbContext db, ISessionManager sessions) : IPermissionService
{
    public bool HasPermission(string code) => sessions.CurrentUser?.HasPermission(code) == true;
    public bool HasAnyPermission(params string[] codes) => codes.Any(HasPermission);
    public bool HasAllPermissions(params string[] codes) => codes.All(HasPermission);
    public async Task RefreshCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var current = sessions.CurrentUser;
        if (current is null) return;
        var user = await db.UserAccounts.AsNoTracking().Include(x => x.Role).ThenInclude(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .Include(x => x.UserRoles).ThenInclude(x => x.Role).ThenInclude(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == current.UserId, cancellationToken);
        if (user is null || !user.IsActive) { sessions.Logout(); return; }
        var roles = user.UserRoles.Select(x => x.Role).Append(user.Role).DistinctBy(x => x.Id).ToArray();
        var roleNames = roles.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permissions = roles.SelectMany(x => x.RolePermissions).Select(x => x.Permission.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roleNames.Contains("Admin")) permissions.UnionWith(AuthSeed.PermissionCodes);
        var sessionData = new UserSessionMapping(user.Id, user.Username, user.FullName,
            user.EmployeeId, roleNames, permissions, current.LoginTime);
        sessions.Login(sessionData.Adapt<UserSession>(AccountMappingConfig.Config));
    }
}
