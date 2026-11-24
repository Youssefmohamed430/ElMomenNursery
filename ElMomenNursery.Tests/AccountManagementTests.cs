using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using ElMomenNursery.Infrastructure.Security;
using ElMomenNursery.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElMomenNursery.Tests;

public sealed class AccountManagementTests
{
    [Fact]
    public async Task Account_results_report_permission_validation_duplicate_and_success_message()
    {
        await using var db = await CreateDb();
        await AuthSeed.InitializeAsync(db);
        var service = new LocalAccountManagementService(db, new TestSessionManager(null), new TestPermissionService(), NullLogger<LocalAccountManagementService>.Instance);
        var roles = await service.GetRolesAsync();
        Assert.True(roles.IsSuccess);
        var role = roles.Value.Single(x => x.Name == "Teacher");

        var weakPassword = await service.CreateAccountAsync("teacher-one", "معلمة", "weak", role.Id);
        Assert.Equal(AccountErrors.WeakPassword, weakPassword.Error);

        var created = await service.CreateAccountAsync("teacher-one", "معلمة", "Password12345", role.Id);
        Assert.True(created.IsSuccess);
        Assert.Equal(AccountErrors.Created.Description, created.Message);
        Assert.Equal(AccountErrors.DuplicateUsername,
            (await service.CreateAccountAsync("teacher-one", "معلمة أخرى", "Password12345", role.Id)).Error);

        var denied = new LocalAccountManagementService(db, new TestSessionManager(null), new TestPermissionService("Users.View"), NullLogger<LocalAccountManagementService>.Instance);
        Assert.Equal(AccountErrors.PermissionDenied, (await denied.GetAccountsAsync()).Error);
    }

    [Fact]
    public async Task Role_reassignment_keeps_roles_that_remain_assigned()
    {
        await using var db = await CreateDb();
        await AuthSeed.InitializeAsync(db);
        var roles = await db.Roles.Where(x => x.Name == "Teacher" || x.Name == "Accountant").ToListAsync();
        var kept = roles.Single(x => x.Name == "Teacher");
        var removed = roles.Single(x => x.Name == "Accountant");
        var user = new UserAccount
        {
            Username = "role-test", UsernameNormalized = "ROLE-TEST", FullName = "اختبار الأدوار",
            PasswordHash = "hash", Role = kept, RoleId = kept.Id
        };
        user.UserRoles.Add(new UserRole { User = user, UserId = user.Id, Role = kept, RoleId = kept.Id });
        user.UserRoles.Add(new UserRole { User = user, UserId = user.Id, Role = removed, RoleId = removed.Id });
        db.UserAccounts.Add(user); await db.SaveChangesAsync();
        var service = new LocalAccountManagementService(db, new TestSessionManager(null), new TestPermissionService(),
            NullLogger<LocalAccountManagementService>.Instance);

        var result = await service.SetUserRolesAsync(user.Id, [kept.Id]);

        Assert.True(result.IsSuccess);
        var remainingRoles = await db.UserRoles.Where(x => x.UserId == user.Id).Select(x => x.RoleId).ToListAsync();
        Assert.Equal([kept.Id], remainingRoles);
        Assert.Equal(kept.Id, (await db.UserAccounts.SingleAsync(x => x.Id == user.Id)).RoleId);
    }

    [Fact]
    public async Task Expired_lockout_resets_attempt_count_before_checking_password()
    {
        await using var db = await CreateDb();
        await AuthSeed.InitializeAsync(db);
        var role = await db.Roles.SingleAsync(x => x.Name == "Teacher");
        var user = new UserAccount
        {
            Username = "expired-lockout", UsernameNormalized = "EXPIRED-LOCKOUT", FullName = "اختبار القفل",
            PasswordHash = "", Role = role, RoleId = role.Id, FailedLoginAttempts = 4,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        user.PasswordHash = new PasswordHasher<UserAccount>().HashPassword(user, "CorrectPassword123");
        db.UserAccounts.Add(user); await db.SaveChangesAsync();
        var auth = new LocalAuthService(db, new TestSessionManager(null), Options.Create(new AuthSecurityOptions()),
            NullLogger<LocalAuthService>.Instance);

        var result = await auth.LoginAsync(user.Username, "WrongPassword123");

        Assert.Equal(CommonErrors.InvalidCredentials, result.Error);
        var saved = await db.UserAccounts.SingleAsync(x => x.Id == user.Id);
        Assert.Equal(1, saved.FailedLoginAttempts);
        Assert.Null(saved.LockoutEnd);
    }

    [Fact]
    public async Task Empty_password_hash_is_not_recognized_as_a_valid_hash()
    {
        await using var db = await CreateDb();
        await AuthSeed.InitializeAsync(db);
        var role = await db.Roles.FirstAsync();
        db.UserAccounts.Add(new UserAccount
        {
            Username = "empty-hash", UsernameNormalized = "EMPTY-HASH", FullName = "حساب اختبار",
            PasswordHash = string.Empty, Role = role, RoleId = role.Id
        });
        await db.SaveChangesAsync();
        var auth = new LocalAuthService(db, new TestSessionManager(null), Options.Create(new AuthSecurityOptions()),
            NullLogger<LocalAuthService>.Instance);

        var result = await auth.NeedsInitialAdministratorSetupAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    private static async Task<NurseryDbContext> CreateDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        var db = new NurseryDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
