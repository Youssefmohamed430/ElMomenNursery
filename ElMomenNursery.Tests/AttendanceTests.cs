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
    public void Attendance_times_must_match_local_attendance_date()
    {
        var pastDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        Assert.Equal(AttendanceErrors.InvalidTimes,
            AttendanceRules.Validate(pastDate, AttendanceStatus.Present, DateTimeOffset.Now, null));
        Assert.Null(AttendanceRules.Validate(pastDate, AttendanceStatus.Present, DateTimeOffset.Now.AddDays(-1), null));
    }

    [Fact]
    public void Correction_requires_reason() => Assert.False(AttendanceRules.HasCorrectionReason("  "));

    [Fact]
    public async Task Attendance_classroom_filter_does_not_require_students_permission()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync();
        var service = new ChildAttendanceService(new TestUnitOfWork(db), NullLogger<ChildAttendanceService>.Instance,
            new TestPermissionService("Students.View"), new TestSessionManager(null));

        var result = await service.GetClassroomsForFilterAsync();

        Assert.True(result.IsSuccess);
    }

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
        var update = await service.UpdateChildAsync(child.Id, new UpdateChildDTO
        {
            FullName = "اسم محدث", Address = "عنوان محدث", EnrollmentDate = child.EnrollmentDate,
            DateOfBirth = child.DateOfBirth, Gender = ChildGender.Girl,
            FatherName = "الأب", FatherPhone = "01000000000", MotherName = "الأم", MotherPhone = "01100000000"
        });
        Assert.True(update.IsSuccess);
        Assert.Equal(ChildErrors.Updated.Description, update.Message);
        var saved = await db.Children.AsNoTracking().SingleAsync(x => x.Id == child.Id);
        Assert.Equal("اسم محدث", saved.FullName);
        Assert.Equal("عنوان محدث", saved.Address);
    }

    [Fact]
    public async Task Employee_update_can_clear_optional_fields()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var employee = new Employee
        {
            FullName = "الموظفة", Qualification = "بكالوريوس", NationalIdNumber = "12345678901234", Address = "عنوان",
            Phone = "01000000000", AlternatePhone = "01100000000", JobDesignation = "معلمة", HireDate = DateOnly.FromDateTime(DateTime.Today)
        };
        db.Employees.Add(employee); await db.SaveChangesAsync();
        var service = new EmployeeService(new TestUnitOfWork(db), NullLogger<EmployeeService>.Instance, new TestPermissionService());
        var update = await service.UpdateEmployee(employee.Id, new UpdateEmployeeDto
        {
            FullName = employee.FullName, Qualification = null, GraduationYear = null, HasExperience = false,
            NationalIdNumber = employee.NationalIdNumber, Address = employee.Address, MaritalStatus = employee.MaritalStatus,
            Phone = employee.Phone, AlternatePhone = null, JobDesignation = employee.JobDesignation, HireDate = employee.HireDate, Salary = employee.Salary
        });
        Assert.True(update.IsSuccess);
        Assert.Equal(EmployeeErrors.Updated.Description, update.Message);
        var saved = await db.Employees.AsNoTracking().SingleAsync();
        Assert.Null(saved.Qualification);
        Assert.Null(saved.AlternatePhone);
    }

    [Fact]
    public async Task Employee_attendance_rejects_date_before_hire_date()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync();
        var employee = new Employee
        {
            FullName = "موظف", NationalIdNumber = "12345678901234", Address = "عنوان",
            Phone = "01000000000", JobDesignation = "إداري", HireDate = DateOnly.FromDateTime(DateTime.Today)
        };
        db.Employees.Add(employee); await db.SaveChangesAsync();
        var service = new EmployeeAttendanceService(new TestUnitOfWork(db), NullLogger<EmployeeAttendanceService>.Instance,
            new TestPermissionService(), new TestSessionManager(null));
        var result = await service.MarkManualAttendanceAsync(new CreateEmployeeAttendanceDto
        {
            EmployeeId = employee.Id, AttendanceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            Status = AttendanceStatus.Absent
        });
        Assert.Equal(AttendanceErrors.PersonNotFound, result.Error);
    }

    [Fact]
    public async Task Child_search_matches_arabic_normalized_hamza()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync();
        var child = new Child
        {
            FullName = "آدم أحمد", Address = "عنوان", EnrollmentDate = DateOnly.FromDateTime(DateTime.Today),
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-4)), FatherName = "الأب", FatherPhone = "01000000000",
            MotherName = "الأم", MotherPhone = "01100000000"
        };
        db.Children.Add(child); await db.SaveChangesAsync();
        var service = new ChildService(new TestUnitOfWork(db), NullLogger<ChildService>.Instance, new TestPermissionService());

        var result = await service.GetChildrenAsync(new ChildFilter { SearchText = "ادم احمد" });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
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
    public async Task Employee_attendance_uses_session_actor_and_returns_employee_name()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options); await db.Database.EnsureCreatedAsync();
        var role = new Role { Name = "Tester" };
        var actor = new UserAccount { Username = "tester", UsernameNormalized = "TESTER", PasswordHash = "hash", Role = role, RoleId = role.Id, FullName = "اسم المستخدم" };
        var employee = new Employee { FullName = "الموظفة", NationalIdNumber = "12345678901234", Address = "عنوان", Phone = "01000000000", JobDesignation = "معلمة", HireDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) };
        db.AddRange(role, actor, employee); await db.SaveChangesAsync();
        var session = new TestSessionManager(new UserSession(actor.Id, "tester", actor.FullName, null, new HashSet<string>(), new HashSet<string>()));
        var service = new EmployeeAttendanceService(new TestUnitOfWork(db), NullLogger<EmployeeAttendanceService>.Instance, new TestPermissionService(), session);
        var result = await service.MarkManualAttendanceAsync(new CreateEmployeeAttendanceDto
        {
            EmployeeId = employee.Id,
            AttendanceDate = DateOnly.FromDateTime(DateTime.Today),
            Status = AttendanceStatus.Present,
            CheckInTime = DateTimeOffset.Now,
            Notes = "  ملاحظة  "
        });
        Assert.True(result.IsSuccess);
        Assert.Equal(employee.FullName, result.Value.EmployeeName);
        Assert.Equal(actor.Id, result.Value.RecordedByUserId);
        Assert.Equal("ملاحظة", result.Value.Notes);
        var saved = await db.EmployeeAttendances.AsNoTracking().SingleAsync();
        Assert.Equal(actor.Id, saved.RecordedByUserId);
        Assert.Equal(AttendanceEntryMethod.Manual, saved.EntryMethod);
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
