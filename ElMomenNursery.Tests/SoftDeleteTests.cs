using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Infrastructure.Persistence;
using ElMomenNursery.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ElMomenNursery.Tests;

public sealed class SoftDeleteTests
{
    [Fact]
    public async Task Soft_delete_hides_from_normal_queries_preserves_history_and_audits()
    {
        await using var db = await CreateDb();
        var child = new Child
        {
            FullName = "طفل تجريبي", Address = "عنوان", EnrollmentDate = new DateOnly(2026, 9, 1), DateOfBirth = new DateOnly(2022, 1, 1),
            Gender = ChildGender.Boy, FatherName = "أب", FatherPhone = "01000000000", MotherName = "أم", MotherPhone = "01000000001"
        };
        db.Children.Add(child);
        await db.SaveChangesAsync();

        db.Children.Remove(child);
        await db.SaveChangesAsync();

        Assert.Empty(await db.Children.ToListAsync());
        var historical = await db.Children.IgnoreQueryFilters().SingleAsync();
        Assert.True(historical.IsDeleted);
        Assert.NotNull(historical.DeletedAt);
        Assert.Single(await db.AuditLogs.Where(x => x.EntityType == nameof(Child) && x.EntityId == child.Id.ToString()).ToListAsync());
    }

    [Fact]
    public async Task Unique_value_can_be_reused_after_employee_is_soft_deleted()
    {
        await using var db = await CreateDb();
        var employee = Employee("12345678901234");
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        db.Employees.Remove(employee);
        await db.SaveChangesAsync();

        db.Employees.Add(Employee(employee.NationalIdNumber));
        await db.SaveChangesAsync();
        Assert.Single(await db.Employees.ToListAsync());
        Assert.Equal(2, await db.Employees.IgnoreQueryFilters().CountAsync());
    }

    private static Employee Employee(string nationalId) => new()
    {
        FullName = "موظفة", NationalIdNumber = nationalId, Address = "عنوان", MaritalStatus = MaritalStatus.Single,
        Phone = "01000000000", JobDesignation = "معلمة", HireDate = new DateOnly(2026, 1, 1)
    };

    private static async Task<NurseryDbContext> CreateDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new SoftDeleteInterceptor(new SessionManager());
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).AddInterceptors(interceptor).Options;
        var db = new NurseryDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
