using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Infrastructure.Persistence;
using ElMomenNursery.Infrastructure.Security;
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
