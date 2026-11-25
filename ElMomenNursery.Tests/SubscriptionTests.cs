using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Services;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Infrastructure.Documents;
using ElMomenNursery.Infrastructure.Persistence;
using ElMomenNursery.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElMomenNursery.Tests;

public sealed class SubscriptionTests
{
    [Theory]
    [InlineData(100, 0, SubscriptionStatus.Pending)]
    [InlineData(100, 20, SubscriptionStatus.PartiallyPaid)]
    [InlineData(100, 100, SubscriptionStatus.Paid)]
    [InlineData(0, 0, SubscriptionStatus.Pending)]
    public void Status_calculation(decimal due, decimal paid, SubscriptionStatus expected) => Assert.Equal(expected, SubscriptionRules.ComputeStatus(due, paid));

    [Fact]
    public async Task Update_is_by_subscription_id_and_persists()
    {
        await using var db = await CreateDb(); var child = Child("تجريبي"); var sub = new Subscription { ChildId = child.Id, Child = child, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddMonths(1), AmountDue = 100 };
        db.AddRange(child, sub); await db.SaveChangesAsync(); var service = Service(db);
        var result = await service.UpdateSubscriptionAsync(sub.Id, new UpdateSubscriptionDto { PeriodStart = sub.PeriodStart, PeriodEnd = sub.PeriodEnd, AmountDue = 120, Notes = "تحديث", Reason = "تصحيح" });
        Assert.True(result.IsSuccess); Assert.Equal(120, (await db.Subscriptions.AsNoTracking().SingleAsync()).AmountDue); Assert.Equal("تحديث", (await db.Subscriptions.AsNoTracking().SingleAsync()).Notes);
    }

    [Fact]
    public async Task Filter_returns_period_intersections_and_tracks_payments()
    {
        await using var db = await CreateDb(); var child = Child("اسم عربي"); var sub = new Subscription { ChildId = child.Id, Child = child, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddMonths(1), AmountDue = 100 };
        db.AddRange(child, sub); await db.SaveChangesAsync(); var service = Service(db);
        var result = await service.GetSubscriptionsAsync(new SubscriptionFilter { PeriodFrom = child.EnrollmentDate.AddDays(10), PeriodTo = child.EnrollmentDate.AddMonths(2), SearchText = "اسم" });
        Assert.True(result.IsSuccess); Assert.Single(result.Value);
    }

    [Fact]
    public async Task Child_lookup_search_reaches_beyond_first_hundred_and_normalizes_arabic()
    {
        await using var db = await CreateDb();
        var children = Enumerable.Range(0, 120).Select(index => Child($"طفل {index:D3}")).ToList();
        var matching = Child("مدرسة التاء المربوطة");
        db.Children.AddRange(children.Append(matching)); await db.SaveChangesAsync();
        var result = await Service(db).GetChildLookupAsync("مدرسه");
        Assert.True(result.IsSuccess);
        var selected = Assert.Single(result.Value);
        Assert.Equal(matching.Id, selected.Id);
    }

    [Fact]
    public async Task Create_with_initial_payment_creates_receipt_and_allocation_atomically()
    {
        await using var db = await CreateDb(); var child = Child("طفل"); db.Children.Add(child); await db.SaveChangesAsync(); var service = Service(db);
        var result = await service.CreateSubscriptionAsync(new CreateSubscriptionDto { ChildId = child.Id, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddMonths(1), AmountDue = 100, InitialPayment = new InitialPaymentDto(25, PaymentMethod.Cash) });
        Assert.True(result.IsSuccess); Assert.Equal(1, await db.Subscriptions.CountAsync()); Assert.Equal(1, await db.Payments.CountAsync()); Assert.Equal(1, await db.PaymentAllocations.CountAsync());
        Assert.Matches("^R-[0-9]{4}-[0-9]{6}$", (await db.Payments.SingleAsync()).ReceiptNumber);
    }

    [Fact]
    public async Task Delete_with_payment_is_rejected_and_cancel_is_preserved()
    {
        await using var db = await CreateDb(); var child = Child("طفل"); var sub = new Subscription { ChildId = child.Id, Child = child, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddMonths(1), AmountDue = 100 };
        db.AddRange(child, sub); await db.SaveChangesAsync(); var service = Service(db);
        Assert.True((await service.RecordPaymentAsync(sub.Id, new RecordPaymentDto(10, PaymentMethod.Cash))).IsSuccess);
        Assert.Equal(SubscriptionErrors.HasPayments, (await service.DeleteSubscriptionAsync(sub.Id)).Error);
        Assert.True((await service.CancelSubscriptionAsync(sub.Id, "طلب ولي الأمر")).IsSuccess);
        Assert.Equal(SubscriptionStatus.Cancelled, (await db.Subscriptions.AsNoTracking().SingleAsync()).Status);
        Assert.Equal(SubscriptionErrors.PaymentNotAllowedForStatus,
            (await service.RecordPaymentAsync(sub.Id, new RecordPaymentDto(10, PaymentMethod.Cash))).Error);
    }

    [Fact]
    public async Task Rejects_negative_due_reversed_period_and_overpayment()
    {
        await using var db = await CreateDb(); var child = Child("طفل"); db.Children.Add(child); await db.SaveChangesAsync(); var service = Service(db);
        var negative = await service.CreateSubscriptionAsync(new CreateSubscriptionDto { ChildId = child.Id, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate, AmountDue = -1 });
        var reversed = await service.CreateSubscriptionAsync(new CreateSubscriptionDto { ChildId = child.Id, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddDays(-1), AmountDue = 100 });
        Assert.Equal(SubscriptionErrors.InvalidAmount, negative.Error); Assert.Equal(SubscriptionErrors.InvalidPeriod, reversed.Error);
        var created = await service.CreateSubscriptionAsync(new CreateSubscriptionDto { ChildId = child.Id, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddMonths(1), AmountDue = 10 });
        var payment = await service.RecordPaymentAsync(created.Value.Id, new RecordPaymentDto(11, PaymentMethod.Cash)); Assert.Equal(SubscriptionErrors.OverpaymentNotAllowed, payment.Error);
    }

    [Fact]
    public async Task Cannot_reduce_due_below_paid_and_cancelled_status_is_preserved_by_update()
    {
        await using var db = await CreateDb(); var child = Child("طفل"); var sub = new Subscription { ChildId = child.Id, Child = child, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddMonths(1), AmountDue = 100 };
        db.AddRange(child, sub); await db.SaveChangesAsync(); var service = Service(db);
        await service.RecordPaymentAsync(sub.Id, new RecordPaymentDto(25, PaymentMethod.Cash));
        var reduced = await service.UpdateSubscriptionAsync(sub.Id, new UpdateSubscriptionDto { PeriodStart = sub.PeriodStart, PeriodEnd = sub.PeriodEnd, AmountDue = 20, Reason = "تصحيح" });
        Assert.Equal(SubscriptionErrors.AmountBelowPaid, reduced.Error);
        await service.CancelSubscriptionAsync(sub.Id, "طلب ولي الأمر");
        var updated = await service.UpdateSubscriptionAsync(sub.Id, new UpdateSubscriptionDto { PeriodStart = sub.PeriodStart, PeriodEnd = sub.PeriodEnd, AmountDue = 100, Reason = "تعديل بيانات" });
        Assert.True(updated.IsSuccess); Assert.Equal(SubscriptionStatus.Cancelled, updated.Value.Status);
    }

    [Fact]
    public async Task Initial_payment_failure_rolls_back_subscription_and_receipt_sequence()
    {
        await using var db = await CreateDb(); var child = Child("طفل"); db.Children.Add(child); await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailPayment BEFORE INSERT ON Payments BEGIN SELECT RAISE(ABORT, 'forced failure'); END;");
        var service = Service(db); var result = await service.CreateSubscriptionAsync(new CreateSubscriptionDto { ChildId = child.Id, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddMonths(1), AmountDue = 50, InitialPayment = new InitialPaymentDto(10, PaymentMethod.Cash) });
        Assert.True(result.IsFailure); Assert.Equal(0, await db.Subscriptions.CountAsync()); Assert.Equal(0, await db.Payments.CountAsync());
        Assert.Empty(await db.AppSettings.Where(x => x.Key.StartsWith("Receipts.Sequence.")).ToListAsync());
    }

    [Fact]
    public async Task Initial_and_next_payment_receive_unique_receipt_numbers()
    {
        await using var db = await CreateDb(); var child = Child("طفل"); db.Children.Add(child); await db.SaveChangesAsync(); var service = Service(db);
        var created = await service.CreateSubscriptionAsync(new CreateSubscriptionDto { ChildId = child.Id, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddMonths(1), AmountDue = 50, InitialPayment = new InitialPaymentDto(10, PaymentMethod.Cash) });
        Assert.NotNull(created.Value.InitialPaymentReceipt);
        await service.RecordPaymentAsync(created.Value.Id, new RecordPaymentDto(10, PaymentMethod.Cash));
        var numbers = await db.Payments.Select(x => x.ReceiptNumber).ToArrayAsync(); Assert.Equal(2, numbers.Distinct().Count());
    }

    [Fact]
    public async Task Overlap_is_a_warning_and_read_permission_is_enforced()
    {
        await using var db = await CreateDb(); var child = Child("طفل"); db.Children.Add(child); await db.SaveChangesAsync(); var service = Service(db);
        var first = await service.CreateSubscriptionAsync(new CreateSubscriptionDto { ChildId = child.Id, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddMonths(1), AmountDue = 50 });
        var second = await service.CreateSubscriptionAsync(new CreateSubscriptionDto { ChildId = child.Id, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddMonths(1), AmountDue = 50 });
        Assert.Equal(SubscriptionErrors.OverlappingPeriod, second.Error);
        var denied = new SubscriptionService(new TestUnitOfWork(db), NullLogger<SubscriptionService>.Instance, new TestPermissionService("Subscriptions.View"), new TestSessionManager(null));
        Assert.Equal(SubscriptionErrors.PermissionDenied, (await denied.GetSubscriptionsAsync(new SubscriptionFilter())).Error);
        Assert.True((await service.CreateSubscriptionAsync(new CreateSubscriptionDto { ChildId = child.Id, PeriodStart = child.EnrollmentDate, PeriodEnd = child.EnrollmentDate.AddMonths(1), AmountDue = 50, ConfirmOverlappingPeriod = true })).IsSuccess);
    }

    [Fact]
    public async Task Receipt_pdf_has_pdf_header_and_arabic_font()
    {
        var service = new ReceiptPdfService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var receipt = new PaymentReceiptDto(Guid.NewGuid(), "R-2026-000001", DateTimeOffset.Now, "طفل تجريبي", new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), 100, 0, 20, 80, PaymentMethod.Cash, null, "مستخدم", null);
        var result = await service.CreateReceiptFileAsync(receipt, false); Assert.True(result.IsSuccess); var bytes = await File.ReadAllBytesAsync(result.Value);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4)); Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task Receipt_copy_uses_a_separate_file_and_preserves_the_original()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new ReceiptPdfService(root);
        var receipt = new PaymentReceiptDto(Guid.NewGuid(), "R-2026-000001", DateTimeOffset.Now, "طفل تجريبي", new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), 100, 0, 20, 80, PaymentMethod.Cash, null, "مستخدم", null);
        var originalResult = await service.CreateReceiptFileAsync(receipt, false); Assert.True(originalResult.IsSuccess); var original = originalResult.Value;
        var originalBytes = await File.ReadAllBytesAsync(original);
        var copyResult = await service.CreateReceiptFileAsync(receipt, true); Assert.True(copyResult.IsSuccess); var copy = copyResult.Value;
        Assert.NotEqual(original, copy);
        Assert.True(File.Exists(original));
        Assert.NotEmpty(await File.ReadAllBytesAsync(copy));
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(original));
    }

    [Fact]
    public async Task Export_seed_v4_is_idempotent_and_preserves_role_edits()
    {
        await using var db = await CreateDb(); await AuthSeed.InitializeAsync(db);
        var manager = await db.Roles.Include(x => x.RolePermissions).ThenInclude(x => x.Permission).SingleAsync(x => x.Name == "Manager");
        var export = manager.RolePermissions.Single(x => x.Permission.Code == "Subscriptions.Export"); db.RolePermissions.Remove(export);
        await db.SaveChangesAsync(); await AuthSeed.InitializeAsync(db);
        Assert.DoesNotContain(manager.RolePermissions, x => x.Permission.Code == "Subscriptions.Export");
        Assert.True(await db.AppSettings.AnyAsync(x => x.Key == "Authentication.RolePermissionsSeeded.V4"));
    }

    private static SubscriptionService Service(NurseryDbContext db) => new(new TestUnitOfWork(db), NullLogger<SubscriptionService>.Instance, new TestPermissionService(), new TestSessionManager(null));
    private static Child Child(string name) { var today = DateOnly.FromDateTime(DateTime.Today); return new Child { FullName = name, Address = "عنوان", EnrollmentDate = today.AddDays(-5), DateOfBirth = today.AddYears(-4), Gender = ChildGender.Girl, FatherName = "الأب", FatherPhone = "01000000000", MotherName = "الأم", MotherPhone = "01100000000" }; }
    private static async Task<NurseryDbContext> CreateDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync(); return db;
    }
}
