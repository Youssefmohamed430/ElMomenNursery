using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Mappings;
using ElMomenNursery.Application.Services;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mapster;

namespace ElMomenNursery.Infrastructure.Security;

public sealed class LocalAccountManagementService(NurseryDbContext db, ISessionManager sessions, IPermissionService permissions,
    ILogger<LocalAccountManagementService> logger) : IAccountManagementService
{
    private static readonly PasswordHasher<UserAccount> Hasher = new();

    public async Task<Result<IReadOnlyList<AccountSummary>>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Users.View", AccountErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var accounts = await db.UserAccounts.AsNoTracking().Include(x => x.Role).Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .OrderBy(x => x.Username).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<AccountSummary>>(accounts.Adapt<List<AccountSummary>>(AccountMappingConfig.Config));
    }

    public async Task<Result<IReadOnlyList<RoleSummary>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        if (!permissions.HasAnyPermission("Roles.Manage", "Users.Manage"))
            return AccountErrors.PermissionDenied;
        var roles = await db.Roles.AsNoTracking().Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<RoleSummary>>(roles.Adapt<List<RoleSummary>>(AccountMappingConfig.Config));
    }

    public async Task<Result> CreateAccountAsync(string username, string fullName, string password, Guid roleId, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Users.Manage", AccountErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        username = (username ?? string.Empty).Trim(); fullName = (fullName ?? string.Empty).Trim();
        if (username.Length is < 3 or > 64 || fullName.Length is < 2 or > 200 || password is not { Length: >= 12 and <= 256 }
            || !password.Any(char.IsLetter) || !password.Any(char.IsDigit)) return AccountErrors.WeakPassword;
        var normalizedUsername = username.ToUpperInvariant();
        if (await db.UserAccounts.AnyAsync(x => x.UsernameNormalized == normalizedUsername, cancellationToken)) return AccountErrors.DuplicateUsername;
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Id == roleId, cancellationToken);
        if (role is null) return AccountErrors.NotFound;
        var user = new UserAccount
        {
            Username = username,
            UsernameNormalized = normalizedUsername,
            FullName = fullName,
            Role = role,
            RoleId = role.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            PasswordHash = string.Empty
        };
        user.PasswordHash = Hasher.HashPassword(user, password);
        user.UserRoles.Add(new UserRole { User = user, UserId = user.Id, Role = role, RoleId = role.Id });
        db.UserAccounts.Add(user);
        var actor = sessions.CurrentUser;
        db.AuthenticationAuditLogs.Add(MapAudit(user.Id, username, "AccountCreated", true,
            $"actorId={actor?.UserId}; account created"));
        var saveError = await SaveChangesErrorAsync(cancellationToken);
        if (saveError is not null) return saveError == PersistenceErrors.Unique ? AccountErrors.DuplicateUsername : saveError;
        logger.LogInformation("Account created; actorId={ActorId}; targetUserId={TargetUserId}", actor?.UserId, user.Id);
        return Result.Success(AccountErrors.Created.Description);
    }

    public async Task<Result> SetAccountActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Users.Manage", AccountErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var actor = sessions.CurrentUser;
        if (!isActive && actor?.UserId == userId)
        {
            logger.LogWarning("Account deactivation rejected by self-deactivation rule; actorId={ActorId}; targetUserId={TargetUserId}", actor.UserId, userId);
            return AccountErrors.InvalidRequest;
        }
        var user = await db.UserAccounts.Include(x => x.Role).Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return AccountErrors.NotFound;
        if (!isActive && user.IsActive && (user.Role.Name == "Admin" || user.UserRoles.Any(x => x.Role.Name == "Admin")))
        {
            var adminRole = await db.Roles.SingleAsync(x => x.Name == "Admin", cancellationToken);
            var activeAdmins = await db.UserAccounts.CountAsync(x => x.IsActive && (x.RoleId == adminRole.Id || x.UserRoles.Any(r => r.RoleId == adminRole.Id)), cancellationToken);
            if (activeAdmins <= 1)
            {
                logger.LogWarning("Account deactivation rejected by last active Admin rule; actorId={ActorId}; targetUserId={TargetUserId}", actor?.UserId, userId);
                return AccountErrors.LastAdministrator;
            }
        }
        user.IsActive = isActive; user.UpdatedAt = DateTimeOffset.UtcNow;
        if (!isActive && sessions.CurrentUser?.UserId == userId) sessions.Logout();
        db.AuthenticationAuditLogs.Add(MapAudit(user.Id, user.Username,
            isActive ? "AccountReactivated" : "AccountDeactivated", true,
            $"actorId={actor?.UserId}; active={isActive}"));
        var saveError = await SaveChangesErrorAsync(cancellationToken);
        if (saveError is not null) return saveError;
        logger.LogInformation("Account active state changed; actorId={ActorId}; targetUserId={TargetUserId}; isActive={IsActive}", actor?.UserId, userId, isActive);
        return Result.Success(AccountErrors.Updated.Description);
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Users.Manage", AccountErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var actor = sessions.CurrentUser;
        if (newPassword is not { Length: >= 12 and <= 256 } || !newPassword.Any(char.IsLetter) || !newPassword.Any(char.IsDigit))
            return AccountErrors.WeakPassword;
        var user = await db.UserAccounts.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return AccountErrors.NotFound;
        user.PasswordHash = Hasher.HashPassword(user, newPassword);
        user.FailedLoginAttempts = 0; user.LockoutEnd = null; user.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuthenticationAuditLogs.Add(MapAudit(user.Id, user.Username, "PasswordResetByAdministrator", true,
            $"actorId={actor?.UserId}; password reset"));
        var saveError = await SaveChangesErrorAsync(cancellationToken);
        if (saveError is not null) return saveError;
        logger.LogInformation("Password reset by administrator; actorId={ActorId}; targetUserId={TargetUserId}", actor?.UserId, userId);
        return Result.Success(AccountErrors.Updated.Description);
    }

    public async Task<Result> SetUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Users.Manage", AccountErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var actor = sessions.CurrentUser;
        if (roleIds.Count == 0) return AccountErrors.InvalidRequest;
        var user = await db.UserAccounts.Include(x => x.Role).Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return AccountErrors.NotFound;
        var roles = await db.Roles.Where(x => roleIds.Distinct().Contains(x.Id)).ToListAsync(cancellationToken);
        if (roles.Count != roleIds.Distinct().Count()) return AccountErrors.InvalidRequest;
        var hadAdmin = user.Role.Name == "Admin" || user.UserRoles.Any(x => x.Role.Name == "Admin");
        var willHaveAdmin = roles.Any(x => x.Name == "Admin");
        if (user.IsActive && hadAdmin && !willHaveAdmin)
        {
            var adminId = await db.Roles.Where(x => x.Name == "Admin").Select(x => x.Id).SingleAsync(cancellationToken);
            var otherAdmins = await db.UserAccounts.CountAsync(x => x.IsActive && x.Id != user.Id
                && (x.RoleId == adminId || x.UserRoles.Any(r => r.RoleId == adminId)), cancellationToken);
            if (otherAdmins == 0)
            {
                logger.LogWarning("Role change rejected by last active Admin rule; actorId={ActorId}; targetUserId={TargetUserId}", actor?.UserId, userId);
                return AccountErrors.LastAdministrator;
            }
        }
        db.UserRoles.RemoveRange(user.UserRoles);
        user.RoleId = roles[0].Id; user.Role = roles[0];
        foreach (var role in roles)
            db.UserRoles.Add(new UserRoleMapping(user, role).Adapt<UserRole>(AccountMappingConfig.Config));
        user.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuthenticationAuditLogs.Add(MapAudit(actor?.UserId, actor?.Username ?? string.Empty, "UserRolesChanged", true,
            $"actorId={actor?.UserId}; targetUserId={userId}; roleCount={roles.Count}"));
        var saveError = await SaveChangesErrorAsync(cancellationToken);
        if (saveError is not null) return saveError;
        logger.LogInformation("User roles changed; actorId={ActorId}; targetUserId={TargetUserId}; roleCount={RoleCount}", actor?.UserId, userId, roles.Count);
        if (sessions.CurrentUser?.UserId == user.Id) await permissions.RefreshCurrentUserAsync(cancellationToken);
        return Result.Success(AccountErrors.Updated.Description);
    }

    public async Task<Result> SetRolePermissionsAsync(Guid roleId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Permissions.Manage", AccountErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var actor = sessions.CurrentUser;
        var role = await db.Roles.Include(x => x.RolePermissions).SingleOrDefaultAsync(x => x.Id == roleId, cancellationToken);
        if (role is null) return AccountErrors.NotFound;
        var normalizedCodes = permissionCodes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (role.Name == "Admin" && !new[] { "Users.Manage", "Roles.Manage", "Permissions.Manage" }.All(normalizedCodes.Contains))
        {
            logger.LogWarning("Role permissions change rejected by Admin permission safeguard; actorId={ActorId}; roleId={RoleId}", actor?.UserId, roleId);
            return AccountErrors.SystemRoleProtected;
        }
        var permissionRows = await db.Permissions.Where(x => normalizedCodes.Contains(x.Code)).ToListAsync(cancellationToken);
        if (permissionRows.Count != normalizedCodes.Length) return AccountErrors.InvalidRequest;
        db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var p in permissionRows) db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
        db.AuthenticationAuditLogs.Add(MapAudit(actor?.UserId, actor?.Username ?? string.Empty, "RolePermissionsChanged", true,
            $"actorId={actor?.UserId}; roleId={roleId}; permissionCount={normalizedCodes.Length}"));
        var saveError = await SaveChangesErrorAsync(cancellationToken);
        if (saveError is not null) return saveError;
        logger.LogInformation("Role permissions changed; actorId={ActorId}; roleId={RoleId}; permissionCount={PermissionCount}", actor?.UserId, roleId, normalizedCodes.Length);
        await permissions.RefreshCurrentUserAsync(cancellationToken);
        return Result.Success(AccountErrors.Updated.Description);
    }

    private static AuthenticationAuditLog MapAudit(Guid? userId, string username, string action, bool success, string? description = null) =>
        new AuthenticationAuditMapping(userId, username, action, success, Description: description)
            .Adapt<AuthenticationAuditLog>(AccountMappingConfig.Config);

    private async Task<Error?> SaveChangesErrorAsync(CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); return null; }
        catch (DbUpdateException exception)
        {
            var error = PersistenceErrorMapper.FromException(exception);
            logger.LogWarning(exception, "Account operation failed during persistence; errorCode={ErrorCode}", error.Code);
            return error;
        }
    }
}
