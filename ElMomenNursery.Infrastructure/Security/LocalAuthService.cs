using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Mappings;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ElMomenNursery.Infrastructure.Configuration;
using Mapster;

namespace ElMomenNursery.Infrastructure.Security;

public sealed class LocalAuthService(NurseryDbContext db, ISessionManager sessions, IOptions<AuthSecurityOptions> options) : IAuthService
{
    private readonly PasswordHasher<UserAccount> _hasher = new();
    private readonly AuthSecurityOptions _options = options.Value;

    public async Task<bool> NeedsInitialAdministratorSetupAsync(CancellationToken cancellationToken = default) =>
        !await db.UserAccounts.AnyAsync(x => x.PasswordHash.StartsWith("AQAAAA"), cancellationToken);

    public async Task<Result<UserSession>> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        username = (username ?? string.Empty).Trim();
        if (username.Length == 0 || string.IsNullOrEmpty(password)) return CommonErrors.InvalidCredentials;

        var normalized = username.ToUpperInvariant();
        var user = await db.UserAccounts.Include(x => x.Role).Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .ThenInclude(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.UsernameNormalized == normalized, cancellationToken);
        if (user is null)
        {
            db.AuthenticationAuditLogs.Add(Audit(null, username, "LoginFailed", false, "InvalidCredentials"));
            await db.SaveChangesAsync(cancellationToken);
            return CommonErrors.InvalidCredentials;
        }
        if (!user.IsActive)
        {
            db.AuthenticationAuditLogs.Add(Audit(user.Id, user.Username, "LoginFailed", false, "InactiveAccount"));
            await db.SaveChangesAsync(cancellationToken);
            return CommonErrors.InvalidCredentials;
        }
        if (user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            db.AuthenticationAuditLogs.Add(Audit(user.Id, user.Username, "LoginFailed", false, "LockedOut"));
            await db.SaveChangesAsync(cancellationToken);
            return CommonErrors.InvalidCredentials;
        }

        var passwordValidation = await ValidatePassword(password, user, cancellationToken);
        if (passwordValidation.Failure is not null) return passwordValidation.Failure;
        var verification = passwordValidation.Verification;

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = _hasher.HashPassword(user, password);
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTimeOffset.UtcNow;
        var roles = user.UserRoles.Select(x => x.Role).Append(user.Role).DistinctBy(x => x.Id).ToArray();
        var permissionCodes = roles.SelectMany(x => x.RolePermissions).Select(x => x.Permission.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roleNames = roles.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roleNames.Contains("Admin")) permissionCodes.UnionWith(AuthSeed.PermissionCodes);
        var sessionData = new UserSessionMapping(user.Id, user.Username,
            string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName,
            user.EmployeeId, roleNames, permissionCodes, DateTimeOffset.UtcNow);
        var session = sessionData.Adapt<UserSession>(AccountMappingConfig.Config);
        db.AuthenticationAuditLogs.Add(Audit(user.Id, user.Username, "LoginSucceeded", true));
        await db.SaveChangesAsync(cancellationToken);
        sessions.Login(session);
        return Result<UserSession>.Success(session);
    }

    private async Task<(PasswordVerificationResult Verification, Result<UserSession>? Failure)> ValidatePassword(
        string password, UserAccount user, CancellationToken cancellationToken)
    {
        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= Math.Max(1, _options.MaxFailedAttempts))
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, _options.LockoutMinutes));
                db.AuthenticationAuditLogs.Add(Audit(user.Id, user.Username, "AccountLocked", false, "TooManyFailedAttempts"));
            }
            db.AuthenticationAuditLogs.Add(Audit(user.Id, user.Username, "LoginFailed", false, "InvalidCredentials"));
            await db.SaveChangesAsync(cancellationToken);
            return (verification, CommonErrors.InvalidCredentials);
        }

        return (verification, null);
    }

    public async Task<Result> CreateInitialAdministratorAsync(string username, string fullName, string password, CancellationToken cancellationToken = default)
    {
        username = (username ?? string.Empty).Trim();
        fullName = (fullName ?? string.Empty).Trim();
        var validation = await ValidateInitialAdministrator(username, fullName, password, cancellationToken);
        if (validation.Failure is not null) return validation.Failure;
        var role = validation.Role!;
        var user = new UserAccountMapping(username, fullName, string.Empty, role, DateTimeOffset.UtcNow)
            .Adapt<UserAccount>(AccountMappingConfig.Config);

        user.PasswordHash = _hasher.HashPassword(user, password);
        user.UserRoles.Add(new UserRoleMapping(user, role).Adapt<UserRole>(AccountMappingConfig.Config));
        db.UserAccounts.Add(user);
        db.AuthenticationAuditLogs.Add(Audit(user.Id, username, "InitialAdministratorCreated", true));
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<(Role? Role, Result? Failure)> ValidateInitialAdministrator(
        string username, string fullName, string password, CancellationToken cancellationToken)
    {
        if (username.Length is < 3 or > 64 || fullName.Length is < 2 or > 200 || !ValidPassword(password))
            return (null, CommonErrors.InvalidRequest);
        if (await db.UserAccounts.AnyAsync(x => x.PasswordHash.StartsWith("AQAAAA"), cancellationToken))
            return (null, CommonErrors.Conflict);
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == "Admin", cancellationToken);
        if (role is null) return (null, CommonErrors.Conflict);
        return (role, null);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var current = sessions.CurrentUser;
        if (current is not null)
        {
            db.AuthenticationAuditLogs.Add(Audit(current.UserId, current.Username, "Logout", true));
            await db.SaveChangesAsync(cancellationToken);
        }
        sessions.Logout();
    }

    public async Task<Result> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var session = sessions.CurrentUser;
        if (session is null) return CommonErrors.InvalidCredentials;
        if (!ValidPassword(newPassword)) return CommonErrors.InvalidRequest;
        var user = await db.UserAccounts.SingleOrDefaultAsync(x => x.Id == session.UserId, cancellationToken);
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
            return CommonErrors.InvalidCredentials;
        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuthenticationAuditLogs.Add(Audit(user.Id, user.Username, "PasswordChanged", true));
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static bool ValidPassword(string? password) => password is { Length: >= 12 and <= 256 }
        && password.Any(char.IsLetter) && password.Any(char.IsDigit);
    private static AuthenticationAuditLog Audit(Guid? userId, string username, string action, bool success, string? reason = null) =>
        new AuthenticationAuditMapping(userId, username, action, success, reason)
            .Adapt<AuthenticationAuditLog>(AccountMappingConfig.Config);
}
