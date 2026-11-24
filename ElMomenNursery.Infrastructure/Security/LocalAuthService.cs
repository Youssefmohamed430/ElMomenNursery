using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Mappings;
using ElMomenNursery.Application.Services;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ElMomenNursery.Infrastructure.Configuration;
using Mapster;
using Microsoft.Extensions.Logging;

namespace ElMomenNursery.Infrastructure.Security;

public sealed class LocalAuthService(NurseryDbContext db, ISessionManager sessions, IOptions<AuthSecurityOptions> options, ILogger<LocalAuthService> logger) : IAuthService
{
    private readonly PasswordHasher<UserAccount> _hasher = new();
    private readonly AuthSecurityOptions _options = options.Value;

    public async Task<Result<bool>> NeedsInitialAdministratorSetupAsync(CancellationToken cancellationToken = default)
    {
        var setupNeeded = !await HasRecognizedPasswordHashAsync(cancellationToken);
        if (setupNeeded) logger.LogInformation("Initial administrator setup mode entered");
        return Result.Success(setupNeeded);
    }

    public async Task<Result<UserSession>> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        username = (username ?? string.Empty).Trim();
        if (username.Length == 0 || string.IsNullOrEmpty(password)) return CommonErrors.InvalidCredentials;

        var normalized = username.ToUpperInvariant();
        var user = await db.UserAccounts.Include(x => x.Role).Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .ThenInclude(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.UsernameNormalized == normalized, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Login rejected for unknown user");
            db.AuthenticationAuditLogs.Add(Audit(null, string.Empty, "LoginFailed", false, "InvalidCredentials"));
            await SaveChangesWithLoggingAsync("UnknownUserLogin", null, cancellationToken);
            return CommonErrors.InvalidCredentials;
        }
        if (!user.IsActive)
        {
            logger.LogWarning("Login rejected for inactive account; userId={UserId}", user.Id);
            db.AuthenticationAuditLogs.Add(Audit(user.Id, user.Username, "LoginFailed", false, "InactiveAccount"));
            await SaveChangesWithLoggingAsync("InactiveAccountLogin", user.Id, cancellationToken);
            return CommonErrors.InvalidCredentials;
        }
        if (user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            logger.LogWarning("Login rejected while account is locked; userId={UserId}", user.Id);
            db.AuthenticationAuditLogs.Add(Audit(user.Id, user.Username, "LoginFailed", false, "LockedOut"));
            await SaveChangesWithLoggingAsync("LockedOutLogin", user.Id, cancellationToken);
            return AuthErrors.LockedOut;
        }

        if (user.LockoutEnd is not null)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            logger.LogInformation("Expired login lockout reset; userId={UserId}", user.Id);
        }

        var passwordValidation = await ValidatePassword(password, user, cancellationToken);
        if (passwordValidation.Failure is not null) return passwordValidation.Failure;
        var verification = passwordValidation.Verification;

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
            logger.LogInformation("Password hash rehashed; userId={UserId}", user.Id);
        }
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
        await SaveChangesWithLoggingAsync("LoginSucceeded", user.Id, cancellationToken);
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
                logger.LogWarning("Account locked after reaching the failed login limit; userId={UserId}", user.Id);
            }
            db.AuthenticationAuditLogs.Add(Audit(user.Id, user.Username, "LoginFailed", false, "InvalidCredentials"));
            if (user.FailedLoginAttempts < Math.Max(1, _options.MaxFailedAttempts))
                logger.LogWarning("Login rejected because credentials were invalid; userId={UserId}", user.Id);
            await SaveChangesWithLoggingAsync("InvalidPasswordLogin", user.Id, cancellationToken);
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
        var user = new UserAccount { Username = username, UsernameNormalized = username.ToUpperInvariant(), FullName = fullName,
            Role = role, RoleId = role.Id, PasswordHash = string.Empty, CreatedAt = DateTimeOffset.UtcNow };
        user.PasswordHash = _hasher.HashPassword(user, password);
        user.UserRoles.Add(new UserRole { User = user, UserId = user.Id, Role = role, RoleId = role.Id });
        db.UserAccounts.Add(user);
        db.AuthenticationAuditLogs.Add(Audit(user.Id, username, "InitialAdministratorCreated", true));
        await SaveChangesWithLoggingAsync("InitialAdministratorCreated", user.Id, cancellationToken);
        logger.LogInformation("Initial administrator created; userId={UserId}", user.Id);
        return Result.Success("تم إنشاء حساب المسؤول.");
    }

    private async Task<(Role? Role, Result? Failure)> ValidateInitialAdministrator(
        string username, string fullName, string password, CancellationToken cancellationToken)
    {
        if (username.Length is < 3 or > 64 || fullName.Length is < 2 or > 200)
            return (null, CommonErrors.InvalidRequest);
        if (!ValidPassword(password)) return (null, AuthErrors.WeakPassword);
        if (await HasRecognizedPasswordHashAsync(cancellationToken))
            return (null, AuthErrors.SetupAlreadyComplete);
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == "Admin", cancellationToken);
        if (role is null) return (null, AuthErrors.SetupUnavailable);
        return (role, null);
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken = default)
    {
        var current = sessions.CurrentUser;
        if (current is not null)
        {
            db.AuthenticationAuditLogs.Add(Audit(current.UserId, current.Username, "Logout", true));
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException exception)
            {
                logger.LogError(exception, "Authentication persistence failed; operation={Operation}; userId={UserId}", "Logout", current.UserId);
                sessions.Logout();
                return PersistenceErrorMapper.FromException(exception);
            }
        }
        sessions.Logout();
        return Result.Success("تم تسجيل الخروج.");
    }

    public async Task<Result> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var session = sessions.CurrentUser;
        if (session is null) return CommonErrors.InvalidCredentials;
        if (!ValidPassword(newPassword)) return AuthErrors.WeakPassword;
        var user = await db.UserAccounts.SingleOrDefaultAsync(x => x.Id == session.UserId, cancellationToken);
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
            return CommonErrors.InvalidCredentials;
        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuthenticationAuditLogs.Add(Audit(user.Id, user.Username, "PasswordChanged", true));
        await SaveChangesWithLoggingAsync("PasswordChanged", user.Id, cancellationToken);
        logger.LogInformation("Password changed; userId={UserId}", user.Id);
        return Result.Success("تم تغيير كلمة المرور.");
    }

    private static bool ValidPassword(string? password) => password is { Length: >= 12 and <= 256 }
        && password.Any(char.IsLetter) && password.Any(char.IsDigit);

    // Decode the Identity hash marker instead of coupling setup detection to its Base64 text prefix.
    // Version 2 and version 3 markers are both recognized so existing accounts keep setup mode disabled.
    private static bool IsRecognizedPasswordHash(string passwordHash)
    {
        try
        {
            var bytes = Convert.FromBase64String(passwordHash);
            if (bytes.Length == 0) return false;
            return (bytes[0] == 0 && bytes.Length == 49) || (bytes[0] == 1 && bytes.Length >= 26);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private async Task<bool> HasRecognizedPasswordHashAsync(CancellationToken cancellationToken) =>
        (await db.UserAccounts.AsNoTracking().Select(user => user.PasswordHash).ToListAsync(cancellationToken))
        .Any(IsRecognizedPasswordHash);

    private async Task SaveChangesWithLoggingAsync(string operation, Guid? userId, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Authentication persistence failed; operation={Operation}; userId={UserId}", operation, userId);
            throw;
        }
    }

    private static AuthenticationAuditLog Audit(Guid? userId, string username, string action, bool success, string? reason = null) =>
        new AuthenticationAuditMapping(userId, username, action, success, reason)
            .Adapt<AuthenticationAuditLog>(AccountMappingConfig.Config);
}
