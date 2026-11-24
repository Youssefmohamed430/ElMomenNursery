using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Services;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Abstractions;
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
        var service = new FinanceService(uow, NullLogger<FinanceService>.Instance, new TestPermissionService(), new TestSessionManager(null));

        var createRes = await service.CreateExpenseAsync(new CreateExpenseDto
        {
            ExpenseDate = DateOnly.FromDateTime(DateTime.Today),
            Category = "كهرباء",
            Amount = 450.50m,
            Description = "فاتورة كهرباء شهرية",
            PaymentReference = "INV-001"
        });

        Assert.True(createRes.IsSuccess, createRes.IsFailure ? createRes.Error.Description : null);
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
        var service = new FinanceService(uow, NullLogger<FinanceService>.Instance, new TestPermissionService(), new TestSessionManager(null));

        var createRes = await service.CreateManualIncomeAsync(new CreateManualIncomeDto
        {
            IncomeDate = DateOnly.FromDateTime(DateTime.Today),
            Source = "تبرع",
            Amount = 1500m,
            Description = "تبرع فاعل خير للأنشطة"
        });

        Assert.True(createRes.IsSuccess, createRes.IsFailure ? createRes.Error.Description : null);
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
        var service = new FinanceService(uow, NullLogger<FinanceService>.Instance, new TestPermissionService(), new TestSessionManager(null));

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

    [Fact]
    public async Task MarkPayrollPaid_RepaysAdvanceAcrossDigitBoundary()
    {
        await using var db = await CreateDbAsync();
        var employee = new Employee
        {
            FullName = "موظف اختبار", NationalIdNumber = "12345678901234", Address = "عنوان",
            Phone = "01000000000", JobDesignation = "إداري", HireDate = new DateOnly(2020, 1, 1), Salary = 2200m
        };
        var advance = new EmployeeAdvance
        {
            Employee = employee, EmployeeId = employee.Id, OriginalAmount = 1000m,
            RemainingAmount = 1000m, AdvanceDate = new DateOnly(2026, 1, 1)
        };
        var payroll = new PayrollRecord
        {
            Employee = employee, EmployeeId = employee.Id, PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31), AgreedSalary = 2200m, AmountPaid = 100m,
            AdvanceDeduction = 400m, IsCalculated = true
        };
        db.AddRange(employee, advance, payroll);
        await db.SaveChangesAsync();

        var uow = new TestUnitOfWork(db);
        var finance = new FinanceService(uow, NullLogger<FinanceService>.Instance,
            new TestPermissionService(), new TestSessionManager(null));
        var service = new PayRollRecordService(uow, NullLogger<PayRollRecordService>.Instance,
            new TestPermissionService(), finance);

        var result = await service.MarkRecordAsPaid(payroll.Id);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        Assert.Equal(600m, (await db.EmployeeAdvances.SingleAsync()).RemainingAmount);
        Assert.Equal(400m, (await db.EmployeeAdvanceRepayments.SingleAsync()).Amount);
    }

    [Fact]
    public async Task AbsenceFine_IsIdempotentForChildAndSubscriptionPeriod()
    {
        await using var db = await CreateDbAsync();
        var child = new Child
        {
            FullName = "طفل اختبار", Address = "عنوان", EnrollmentDate = new DateOnly(2026, 1, 1),
            DateOfBirth = new DateOnly(2022, 1, 1), FatherName = "الأب", FatherPhone = "01000000000",
            MotherName = "الأم", MotherPhone = "01000000000"
        };
        db.Add(new Subscription
        {
            Child = child, ChildId = child.Id, PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31), AmountDue = 500m
        });
        await db.SaveChangesAsync();
        var service = new FinanceService(new TestUnitOfWork(db), NullLogger<FinanceService>.Instance,
            new TestPermissionService(), new TestSessionManager(null));

        var first = await service.CreateAbsenceFineAsync(child.Id, new DateOnly(2026, 1, 20));
        var duplicate = await service.CreateAbsenceFineAsync(child.Id, new DateOnly(2026, 1, 21));

        Assert.True(first.IsSuccess, first.IsFailure ? first.Error.Description : null);
        Assert.Equal(50m, first.Value.Amount);
        Assert.Equal(FinanceErrors.AbsenceFineAlreadyExists, duplicate.Error);
        Assert.Single(await db.ManualIncomes.ToListAsync());
    }

    [Fact]
    public async Task FinanceSummaryMatchesLists_WhenChildAndEmployeeAreSoftDeleted()
    {
        await using var db = await CreateDbAsync();
        var child = new Child
        {
            FullName = "طفل محذوف", Address = "عنوان", EnrollmentDate = new DateOnly(2022, 1, 1),
            DateOfBirth = new DateOnly(2020, 1, 1), FatherName = "الأب", FatherPhone = "01000000000",
            MotherName = "الأم", MotherPhone = "01000000000", IsDeleted = true
        };
        var payment = new Payment
        {
            Child = child, ChildId = child.Id, Amount = 250m, PaidAt = DateTimeOffset.Now,
            PaymentMethod = ElMomenNursery.Domain.Enums.PaymentMethod.Cash,
            ReceiptNumber = "R-TEST-1", Status = ElMomenNursery.Domain.Enums.PaymentStatus.Posted
        };
        var employee = new Employee
        {
            FullName = "موظف محذوف", NationalIdNumber = "12345678901234", Address = "عنوان",
            Phone = "01000000000", JobDesignation = "إداري", HireDate = new DateOnly(2020, 1, 1), IsDeleted = true
        };
        var payroll = new PayrollRecord
        {
            Employee = employee, EmployeeId = employee.Id, PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31), AgreedSalary = 900m, AmountPaid = 900m, IsPaid = true
        };
        db.AddRange(child, payment, employee, payroll);
        await db.SaveChangesAsync();
        var service = new FinanceService(new TestUnitOfWork(db), NullLogger<FinanceService>.Instance,
            new TestPermissionService(), new TestSessionManager(null));

        var summary = await service.GetSummaryAsync(new FinanceDateFilter());
        var incomeRows = await service.GetIncomeTransactionsAsync(new FinanceDateFilter());
        var expenseRows = await service.GetExpenseTransactionsAsync(new FinanceDateFilter());

        Assert.True(summary.IsSuccess);
        Assert.True(incomeRows.IsSuccess);
        Assert.True(expenseRows.IsSuccess);
        Assert.Equal(250m, summary.Value.TotalSubscriptionIncome);
        Assert.Equal(summary.Value.TotalSubscriptionIncome, incomeRows.Value.Sum(x => x.Amount));
        Assert.Equal(900m, summary.Value.TotalPayrollExpense);
        Assert.Equal(summary.Value.TotalPayrollExpense, expenseRows.Value.Sum(x => x.Amount));
    }

    [Fact]
    public async Task DeletedPayrollRecords_AreIncludedInDeletedHistory()
    {
        await using var db = await CreateDbAsync();
        var employee = new Employee
        {
            FullName = "موظف مؤرشف", NationalIdNumber = "12345678901234", Address = "عنوان",
            Phone = "01000000000", JobDesignation = "إداري", HireDate = new DateOnly(2020, 1, 1), IsDeleted = true
        };
        var record = new PayrollRecord
        {
            Employee = employee, EmployeeId = employee.Id, PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31), AgreedSalary = 1000m, IsDeleted = true
        };
        db.AddRange(employee, record); await db.SaveChangesAsync();
        var uow = new TestUnitOfWork(db);
        var finance = new FinanceService(uow, NullLogger<FinanceService>.Instance,
            new TestPermissionService(), new TestSessionManager(null));
        var service = new PayRollRecordService(uow, NullLogger<PayRollRecordService>.Instance,
            new TestPermissionService(), finance);

        var deleted = await service.GetDeletedPayrollRecordsAsync();

        Assert.True(deleted.IsSuccess);
        Assert.Single(deleted.Value);
        Assert.Equal("موظف مؤرشف", deleted.Value[0].EmployeeName);
    }
}
