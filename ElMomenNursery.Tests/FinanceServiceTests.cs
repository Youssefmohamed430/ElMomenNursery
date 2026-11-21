using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Services;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ElMomenNursery.Tests;

public sealed class FinanceServiceTests
{
    private static async Task<NurseryDbContext> CreateDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new NurseryDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static UserSession CreateAdminSession() =>
        new(Guid.NewGuid(), "admin", "مدير النظام", null, new HashSet<string> { "Admin" }, new HashSet<string> { "*" }, DateTimeOffset.Now);

    [Fact]
    public async Task CreateManualExpense_And_Summary_CalculatesCorrectly()
    {
        await using var db = await CreateDbAsync();
        var uow = new TestUnitOfWork(db);
        var service = new FinanceService(uow, NullLogger<FinanceService>.Instance, new TestPermissionService(), new TestSessionManager(CreateAdminSession()));

        var createRes = await service.CreateExpenseAsync(new CreateExpenseDto
        {
            ExpenseDate = DateOnly.FromDateTime(DateTime.Today),
            Category = "كهرباء",
            Amount = 450.50m,
            Description = "فاتورة كهرباء شهرية",
            PaymentReference = "INV-001"
        });

        Assert.True(createRes.IsSuccess);
        Assert.Equal(450.50m, createRes.Value.Amount);
        Assert.True(createRes.Value.IsEditable);

        var summaryRes = await service.GetSummaryAsync(new FinanceDateFilter());
        Assert.True(summaryRes.IsSuccess);
        Assert.Equal(450.50m, summaryRes.Value.TotalManualExpense);
        Assert.Equal(450.50m, summaryRes.Value.TotalExpenses);
        Assert.Equal(-450.50m, summaryRes.Value.NetBalance);
    }

    [Fact]
    public async Task CreateManualIncome_And_Summary_CalculatesCorrectly()
    {
        await using var db = await CreateDbAsync();
        var uow = new TestUnitOfWork(db);
        var service = new FinanceService(uow, NullLogger<FinanceService>.Instance, new TestPermissionService(), new TestSessionManager(CreateAdminSession()));

        var createRes = await service.CreateManualIncomeAsync(new CreateManualIncomeDto
        {
            IncomeDate = DateOnly.FromDateTime(DateTime.Today),
            Source = "تبرع",
            Amount = 1500m,
            Description = "تبرع فاعل خير للأنشطة"
        });

        Assert.True(createRes.IsSuccess);
        Assert.Equal(1500m, createRes.Value.Amount);
        Assert.True(createRes.Value.IsEditable);

        var summaryRes = await service.GetSummaryAsync(new FinanceDateFilter());
        Assert.True(summaryRes.IsSuccess);
        Assert.Equal(1500m, summaryRes.Value.TotalManualIncome);
        Assert.Equal(1500m, summaryRes.Value.TotalIncome);
        Assert.Equal(1500m, summaryRes.Value.NetBalance);
    }

    [Fact]
    public async Task VoidExpense_ExcludesFromSummary_And_ExpensesList()
    {
        await using var db = await CreateDbAsync();
        var uow = new TestUnitOfWork(db);
        var service = new FinanceService(uow, NullLogger<FinanceService>.Instance, new TestPermissionService(), new TestSessionManager(CreateAdminSession()));

        var expense = await service.CreateExpenseAsync(new CreateExpenseDto
        {
            ExpenseDate = DateOnly.FromDateTime(DateTime.Today),
            Category = "صيانة",
            Amount = 200m,
            Description = "إصلاح باب"
        });

        var deleteRes = await service.DeleteExpenseAsync(expense.Value.Id);
        Assert.True(deleteRes.IsSuccess);

        var summaryRes = await service.GetSummaryAsync(new FinanceDateFilter());
        Assert.True(summaryRes.IsSuccess);
        Assert.Equal(0m, summaryRes.Value.TotalExpenses);

        var listRes = await service.GetExpenseTransactionsAsync(new FinanceDateFilter());
        Assert.True(listRes.IsSuccess);
        Assert.Empty(listRes.Value);
    }
}
