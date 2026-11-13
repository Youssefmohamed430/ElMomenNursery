using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ElMomenNursery.Infrastructure.Security;

public sealed class LocalAccountManagementService(NurseryDbContext db, ISessionManager sessions, IPermissionService permissions) : IAccountManagementService
{
    private static readonly PasswordHasher<UserAccount> Hasher = new();

    public async Task<IReadOnlyList<AccountSummary>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        permissions.EnsurePermission("Users.View");
        return await db.UserAccounts.AsNoTracking().Include(x => x.Role).Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .OrderBy(x => x.Username).Select(x => new AccountSummary(x.Id, x.Username, x.FullName, x.IsActive,
                x.UserRoles.Select(r => r.Role.Name).Append(x.Role.Name).Distinct().ToArray())).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleSummary>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        if (!permissions.HasAnyPermission("Roles.Manage", "Users.Manage"))
            throw new UnauthorizedAccessException("الصلاحية المطلوبة غير متاحة: Roles.Manage");
        return await db.Roles.AsNoTracking().Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .OrderBy(x => x.Name).Select(x => new RoleSummary(x.Id, x.Name, x.RolePermissions.Select(p => p.Permission.Code).ToArray()))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result> CreateAccountAsync(string username, string fullName, string password, Guid roleId, CancellationToken cancellationToken = default)
    {
        permissions.EnsurePermission("Users.Manage");
        username = (username ?? string.Empty).Trim(); fullName = (fullName ?? string.Empty).Trim();
        if (username.Length is < 3 or > 64 || fullName.Length is < 2 or > 200 || password is not { Length: >= 12 and <= 256 }
            || !password.Any(char.IsLetter) || !password.Any(char.IsDigit)) return CommonErrors.InvalidRequest;
        var normalized = username.ToUpperInvariant();
        if (await db.UserAccounts.AnyAsync(x => x.UsernameNormalized == normalized, cancellationToken)) return CommonErrors.Conflict;
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Id == roleId, cancellationToken);
        if (role is null) return CommonErrors.NotFound;
        var user = new UserAccount { Username = username, UsernameNormalized = normalized, FullName = fullName,
            PasswordHash = string.Empty, Role = role, RoleId = role.Id, IsActive = true };
        user.PasswordHash = Hasher.HashPassword(user, password);
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role });
        db.UserAccounts.Add(user);
        db.AuthenticationAuditLogs.Add(new AuthenticationAuditLog { UserId = user.Id, Username = username, Action = "AccountCreated", Success = true });
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SetAccountActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        permissions.EnsurePermission("Users.Manage");
        var actor = sessions.CurrentUser;
        if (!isActive && actor?.UserId == userId) return CommonErrors.Conflict;
        var user = await db.UserAccounts.Include(x => x.Role).Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return CommonErrors.NotFound;
        if (!isActive && user.IsActive && (user.Role.Name == "Admin" || user.UserRoles.Any(x => x.Role.Name == "Admin")))
        {
            var adminRole = await db.Roles.SingleAsync(x => x.Name == "Admin", cancellationToken);
            var activeAdmins = await db.UserAccounts.CountAsync(x => x.IsActive && (x.RoleId == adminRole.Id || x.UserRoles.Any(r => r.RoleId == adminRole.Id)), cancellationToken);
            if (activeAdmins <= 1) return CommonErrors.Conflict;
        }
        user.IsActive = isActive; user.UpdatedAt = DateTimeOffset.UtcNow;
        if (!isActive && sessions.CurrentUser?.UserId == userId) sessions.Logout();
        db.AuthenticationAuditLogs.Add(new AuthenticationAuditLog { UserId = user.Id, Username = user.Username,
            Action = isActive ? "AccountReactivated" : "AccountDeactivated", Success = true });
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        permissions.EnsurePermission("Users.Manage");
        if (newPassword is not { Length: >= 12 and <= 256 } || !newPassword.Any(char.IsLetter) || !newPassword.Any(char.IsDigit))
            return CommonErrors.InvalidRequest;
        var user = await db.UserAccounts.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return CommonErrors.NotFound;
        user.PasswordHash = Hasher.HashPassword(user, newPassword);
        user.FailedLoginAttempts = 0; user.LockoutEnd = null; user.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuthenticationAuditLogs.Add(new AuthenticationAuditLog { UserId = user.Id, Username = user.Username,
            Action = "PasswordResetByAdministrator", Success = true });
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SetUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        permissions.EnsurePermission("Users.Manage");
        if (roleIds.Count == 0) return CommonErrors.InvalidRequest;
        var user = await db.UserAccounts.Include(x => x.Role).Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return CommonErrors.NotFound;
        var roles = await db.Roles.Where(x => roleIds.Distinct().Contains(x.Id)).ToListAsync(cancellationToken);
        if (roles.Count != roleIds.Distinct().Count()) return CommonErrors.InvalidRequest;
        var hadAdmin = user.Role.Name == "Admin" || user.UserRoles.Any(x => x.Role.Name == "Admin");
        var willHaveAdmin = roles.Any(x => x.Name == "Admin");
        if (user.IsActive && hadAdmin && !willHaveAdmin)
        {
            var adminId = await db.Roles.Where(x => x.Name == "Admin").Select(x => x.Id).SingleAsync(cancellationToken);
            var otherAdmins = await db.UserAccounts.CountAsync(x => x.IsActive && x.Id != user.Id
                && (x.RoleId == adminId || x.UserRoles.Any(r => r.RoleId == adminId)), cancellationToken);
            if (otherAdmins == 0) return CommonErrors.Conflict;
        }
        db.UserRoles.RemoveRange(user.UserRoles);
        user.RoleId = roles[0].Id; user.Role = roles[0];
        foreach (var role in roles) db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        if (sessions.CurrentUser?.UserId == user.Id) await permissions.RefreshCurrentUserAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SetRolePermissionsAsync(Guid roleId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        permissions.EnsurePermission("Permissions.Manage");
        var role = await db.Roles.Include(x => x.RolePermissions).SingleOrDefaultAsync(x => x.Id == roleId, cancellationToken);
        if (role is null) return CommonErrors.NotFound;
        var normalizedCodes = permissionCodes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (role.Name == "Admin" && !new[] { "Users.Manage", "Roles.Manage", "Permissions.Manage" }.All(normalizedCodes.Contains))
            return CommonErrors.Conflict;
        var permissionRows = await db.Permissions.Where(x => normalizedCodes.Contains(x.Code)).ToListAsync(cancellationToken);
        if (permissionRows.Count != normalizedCodes.Length) return CommonErrors.InvalidRequest;
        db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var p in permissionRows) db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
        await db.SaveChangesAsync(cancellationToken);
        await permissions.RefreshCurrentUserAsync(cancellationToken);
        return Result.Success();
    }
}
