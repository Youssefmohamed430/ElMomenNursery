using System.Linq.Expressions;
using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Services;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Infrastructure.Persistence;
using ElMomenNursery.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElMomenNursery.Tests;

public sealed class AttendanceRulesTests
{
    [Fact]
    public void Future_date_is_rejected() => Assert.Equal(AttendanceErrors.InvalidDate,
        AttendanceRules.Validate(DateOnly.FromDateTime(DateTime.Today.AddDays(1)), AttendanceStatus.Present, DateTimeOffset.Now, null));

    [Fact]
    public void Check_out_before_check_in_is_rejected()
    {
        var date = DateOnly.FromDateTime(DateTime.Today);
        Assert.Equal(AttendanceErrors.CheckOutBeforeCheckIn,
            AttendanceRules.Validate(date, AttendanceStatus.Present, DateTimeOffset.Now, DateTimeOffset.Now.AddMinutes(-1)));
    }

    [Theory]
    [InlineData(AttendanceStatus.Absent)]
    [InlineData(AttendanceStatus.Excused)]
    public void Absent_states_reject_times(AttendanceStatus status) => Assert.Equal(AttendanceErrors.InvalidTimes,
        AttendanceRules.Validate(DateOnly.FromDateTime(DateTime.Today), status, DateTimeOffset.Now, null));

    [Fact]
    public void Present_requires_check_in() => Assert.Equal(AttendanceErrors.InvalidTimes,
        AttendanceRules.Validate(DateOnly.FromDateTime(DateTime.Today), AttendanceStatus.Present, null, null));

    [Fact]
    public void Correction_requires_reason() => Assert.False(AttendanceRules.HasCorrectionReason("  "));

    [Fact]
    public async Task Child_service_update_persists_tracked_changes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var child = new Child
        {
            FullName = "اسم قديم", Address = "عنوان", EnrollmentDate = DateOnly.FromDateTime(DateTime.Today),
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-4)), Gender = ChildGender.Girl,
            FatherName = "الأب", FatherPhone = "01000000000", MotherName = "الأم", MotherPhone = "01100000000"
        };
        db.Children.Add(child); await db.SaveChangesAsync();
        var service = new ChildService(new TestUnitOfWork(db), NullLogger<ChildService>.Instance, new TestPermissionService());
        await service.UpdateChildAsync(child.Id, new UpdateChildDTO
        {
            FullName = "اسم محدث", Address = "عنوان محدث", EnrollmentDate = child.EnrollmentDate,
            DateOfBirth = child.DateOfBirth, Gender = ChildGender.Girl,
            FatherName = "الأب", FatherPhone = "01000000000", MotherName = "الأم", MotherPhone = "01100000000"
        });
        var saved = await db.Children.AsNoTracking().SingleAsync(x => x.Id == child.Id);
        Assert.Equal("اسم محدث", saved.FullName);
        Assert.Equal("عنوان محدث", saved.Address);
    }

    [Fact]
    public async Task Child_attendance_uses_session_actor_and_returns_conflict_for_duplicate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync();
        var role = new Role { Name = "Tester" };
        var actor = new UserAccount { Username = "tester", UsernameNormalized = "TESTER", PasswordHash = "hash", Role = role, RoleId = role.Id, FullName = "اسم المستخدم" };
        var child = NewChild("الطفل", DateOnly.FromDateTime(DateTime.Today));
        db.AddRange(role, actor, child); await db.SaveChangesAsync();
        var session = new TestSessionManager(new UserSession(actor.Id, "tester", actor.FullName, null, new HashSet<string>(), new HashSet<string>()));
        var service = new ChildAttendanceService(new TestUnitOfWork(db), NullLogger<ChildAttendanceService>.Instance, new TestPermissionService(), session);
        var dto = new CreateChildAttendanceDto(child.Id, DateOnly.FromDateTime(DateTime.Today), AttendanceStatus.Present, DateTimeOffset.Now);
        var first = await service.MarkManualAttendanceAsync(dto);
        var second = await service.MarkManualAttendanceAsync(dto);
        Assert.True(first.IsSuccess);
        Assert.Equal(AttendanceErrors.AlreadyRecorded, second.Error);
        var saved = await db.ChildAttendances.AsNoTracking().SingleAsync();
        Assert.Equal(actor.Id, saved.RecordedByUserId);
    }

    [Fact]
    public async Task Child_daily_list_excludes_future_enrollment_and_filters_classroom()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var included = NewChild("داخل الفصل", today.AddDays(-1));
        var excluded = NewChild("تسجيل مستقبلي", today.AddDays(1));
        var classroom = new Classroom { Name = "فصل النحل" };
        db.AddRange(included, excluded, classroom);
        db.ChildClassEnrollments.Add(new ChildClassEnrollment { ChildId = included.Id, Child = included, ClassroomId = classroom.Id, Classroom = classroom, StartDate = today.AddDays(-1) });
        await db.SaveChangesAsync();
        var service = new ChildAttendanceService(new TestUnitOfWork(db), NullLogger<ChildAttendanceService>.Instance, new TestPermissionService(), new TestSessionManager(null));
        var result = await service.GetDailyAttendanceAsync(new ChildDailyAttendanceQuery(today, classroom.Id));
        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value);
        Assert.Equal(included.Id, row.ChildId);
        Assert.Equal("فصل النحل", row.ClassroomNames);
    }

    [Fact]
    public async Task Child_attendance_read_is_denied_without_permission()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync();
        var service = new ChildAttendanceService(new TestUnitOfWork(db), NullLogger<ChildAttendanceService>.Instance, new TestPermissionService("Attendance.View"), new TestSessionManager(null));
        var result = await service.GetDailyAttendanceAsync(new ChildDailyAttendanceQuery(DateOnly.FromDateTime(DateTime.Today)));
        Assert.Equal(AttendanceErrors.PermissionDenied, result.Error);
    }

    [Fact]
    public async Task Mark_all_present_skips_existing_records_and_can_be_retried()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.Children.AddRange(NewChild("أ", today.AddDays(-1)), NewChild("ب", today.AddDays(-1)));
        await db.SaveChangesAsync();
        var service = new ChildAttendanceService(new TestUnitOfWork(db), NullLogger<ChildAttendanceService>.Instance, new TestPermissionService(), new TestSessionManager(null));
        var first = await service.MarkAllPresentAsync(today, null);
        var second = await service.MarkAllPresentAsync(today, null);
        Assert.Equal(2, first.Value);
        Assert.Equal(0, second.Value);
        Assert.Equal(2, await db.ChildAttendances.CountAsync());
    }

    [Fact]
    public async Task Employee_attendance_excludes_employees_hired_after_requested_date()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync();
        db.Employees.Add(new Employee { FullName = "موظف مستقبلي", NationalIdNumber = "12345678901234", Address = "عنوان", Phone = "01000000000", JobDesignation = "معلمة", HireDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) });
        await db.SaveChangesAsync();
        var service = new EmployeeAttendanceService(new TestUnitOfWork(db), NullLogger<EmployeeAttendanceService>.Instance, new TestPermissionService(), new TestSessionManager(null));
        var result = await service.GetDailyAttendanceAsync(DateOnly.FromDateTime(DateTime.Today));
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Permission_seed_v2_is_idempotent_and_preserves_admin_edits()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync();
        await AuthSeed.InitializeAsync(db);
        var manager = await db.Roles.Include(x => x.RolePermissions).ThenInclude(x => x.Permission).SingleAsync(x => x.Name == "Manager");
        var edit = manager.RolePermissions.Single(x => x.Permission.Code == "EmployeeAttendance.Edit");
        db.RolePermissions.Remove(edit); await db.SaveChangesAsync();
        await AuthSeed.InitializeAsync(db);
        Assert.DoesNotContain(manager.RolePermissions, x => x.Permission.Code == "EmployeeAttendance.Edit");
        Assert.True(await db.AppSettings.AnyAsync(x => x.Key == "Authentication.RolePermissionsSeeded.V2"));
    }

    private static Child NewChild(string name, DateOnly enrollment) => new()
    {
        FullName = name, Address = "عنوان", EnrollmentDate = enrollment, DateOfBirth = enrollment.AddYears(-4),
        Gender = ChildGender.Girl, FatherName = "الأب", FatherPhone = "01000000000", MotherName = "الأم", MotherPhone = "01100000000"
    };

}
