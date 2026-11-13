using ElMomenNursery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ElMomenNursery.Infrastructure.Persistence;

public sealed class NurseryDbContext(DbContextOptions<NurseryDbContext> options) : DbContext(options)
{
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<AuthenticationAuditLog> AuthenticationAuditLogs => Set<AuthenticationAuditLog>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<ChildClassEnrollment> ChildClassEnrollments => Set<ChildClassEnrollment>();
    public DbSet<ClassTeacherAssignment> ClassTeacherAssignments => Set<ClassTeacherAssignment>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<ChildAttendance> ChildAttendances => Set<ChildAttendance>();
    public DbSet<EmployeeAttendance> EmployeeAttendances => Set<EmployeeAttendance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AttendanceDevice> AttendanceDevices => Set<AttendanceDevice>();
    public DbSet<DeviceAttendanceEvent> DeviceAttendanceEvents => Set<DeviceAttendanceEvent>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateOnly>().HaveConversion<DateOnlyConverter, DateOnlyComparer>();
        configurationBuilder.Properties<DateOnly?>().HaveConversion<NullableDateOnlyConverter, NullableDateOnlyComparer>();
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToUnixMillisecondsConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<NullableDateTimeOffsetConverter>();
        configurationBuilder.Properties<Guid>().HaveConversion<string>();
        configurationBuilder.Properties<Guid?>().HaveConversion<NullableGuidConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NurseryDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    private sealed class DateOnlyConverter() : ValueConverter<DateOnly, string>(v => v.ToString("yyyy-MM-dd"), v => DateOnly.Parse(v));
    private sealed class NullableDateOnlyConverter() : ValueConverter<DateOnly?, string?>(v => v.HasValue ? v.Value.ToString("yyyy-MM-dd") : null, v => v == null ? null : DateOnly.Parse(v));
    private sealed class DateOnlyComparer() : ValueComparer<DateOnly>((a, b) => a.DayNumber == b.DayNumber, v => v.GetHashCode(), v => v);
    private sealed class NullableDateOnlyComparer() : ValueComparer<DateOnly?>((a, b) => a == b, v => v.GetHashCode(), v => v);
    private sealed class NullableDateTimeOffsetConverter() : ValueConverter<DateTimeOffset?, long?>(v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : null, v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
    private sealed class NullableGuidConverter() : ValueConverter<Guid?, string?>(v => v.HasValue ? v.Value.ToString("D") : null, v => v == null ? null : Guid.Parse(v));
    private sealed class DateTimeOffsetToUnixMillisecondsConverter() : ValueConverter<DateTimeOffset, long>(v => v.ToUnixTimeMilliseconds(), v => DateTimeOffset.FromUnixTimeMilliseconds(v));
}
