using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElMomenNursery.Infrastructure.Persistence.Configurations;

internal sealed class LookupItemConfiguration : IEntityTypeConfiguration<LookupItem>
{
    public void Configure(EntityTypeBuilder<LookupItem> b)
    {
        b.ToTable("LookupItems"); b.HasKey(x => x.Id);
        b.Property(x => x.Category).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.HasIndex(x => new { x.Category, x.Name }).IsUnique().HasFilter("\"IsDeleted\" = 0").HasDatabaseName("UX_LookupItems_Category_Name");
    }
}

internal sealed class ChildConfiguration : IEntityTypeConfiguration<Child>
{
    public void Configure(EntityTypeBuilder<Child> b)
    {
        b.ToTable("Children", t => t.HasCheckConstraint("CK_Children_DateOrder", "\"DateOfBirth\" <= \"EnrollmentDate\""));
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).IsRequired();
        b.Property(x => x.Address).IsRequired();
        b.Property(x => x.FatherName).IsRequired();
        b.Property(x => x.FatherPhone).IsRequired();
        b.Property(x => x.MotherName).IsRequired();
        b.Property(x => x.MotherPhone).IsRequired();
        b.Property(x => x.Gender).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasIndex(x => x.FullName).HasDatabaseName("IX_Children_FullName");
        b.HasMany(x => x.Subscriptions).WithOne(x => x.Child).HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.ClassEnrollments).WithOne(x => x.Child).HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Attachments).WithOne(x => x.Child).HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Payments).WithOne(x => x.Child).HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Attendances).WithOne(x => x.Child).HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("Employees", t => t.HasCheckConstraint("CK_Employees_SalaryNonNegative", "CAST(\"Salary\" AS REAL) >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).IsRequired();
        b.Property(x => x.NationalIdNumber).IsRequired();
        b.Property(x => x.Address).IsRequired();
        b.Property(x => x.MaritalStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.Phone).IsRequired();
        b.Property(x => x.JobDesignation).IsRequired();
        b.Property(x => x.Salary).HasPrecision(18, 2);
        b.HasIndex(x => x.NationalIdNumber).IsUnique().HasFilter("\"IsDeleted\" = 0").HasDatabaseName("UX_Employees_NationalIdNumber");
        b.HasIndex(x => x.FullName).HasDatabaseName("IX_Employees_FullName");
        b.HasMany(x => x.TeachingAssignments).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Attachments).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Attendances).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.PayrollRecords).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired();
        b.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = 0").HasDatabaseName("UX_Roles_Name");
        b.HasMany(x => x.UserAccounts).WithOne(x => x.Role).HasForeignKey(x => x.RoleId).IsRequired().OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("Permissions"); b.HasKey(x => x.Id);
        b.Property(x => x.Code).IsRequired().HasMaxLength(120);
        b.Property(x => x.Name).IsRequired().HasMaxLength(160);
        b.Property(x => x.Module).IsRequired().HasMaxLength(80);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("UserRoles"); b.HasKey(x => new { x.UserId, x.RoleId });
        b.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("RolePermissions"); b.HasKey(x => new { x.RoleId, x.PermissionId });
        b.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AuthenticationAuditLogConfiguration : IEntityTypeConfiguration<AuthenticationAuditLog>
{
    public void Configure(EntityTypeBuilder<AuthenticationAuditLog> b)
    {
        b.ToTable("AuthenticationAuditLogs"); b.HasKey(x => x.Id);
        b.Property(x => x.Username).IsRequired().HasMaxLength(256);
        b.Property(x => x.Action).IsRequired().HasMaxLength(64);
        b.Property(x => x.FailureReason).HasMaxLength(120);
        b.HasIndex(x => x.Timestamp);
        b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> b)
    {
        b.ToTable("UserAccounts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Username).IsRequired();
        b.Property(x => x.UsernameNormalized).HasMaxLength(256).IsRequired();
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.UsernameNormalized).IsUnique().HasDatabaseName("UX_UserAccounts_UsernameNormalized");
        b.HasIndex(x => x.EmployeeId).IsUnique().HasDatabaseName("UX_UserAccounts_EmployeeId");
        b.HasOne(x => x.Employee).WithOne(x => x.UserAccount).HasForeignKey<UserAccount>(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> b)
    {
        b.ToTable("Classrooms");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired();
        b.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = 0").HasDatabaseName("UX_Classrooms_Name");
        b.HasMany(x => x.ChildEnrollments).WithOne(x => x.Classroom).HasForeignKey(x => x.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.TeacherAssignments).WithOne(x => x.Classroom).HasForeignKey(x => x.ClassroomId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ChildClassEnrollmentConfiguration : IEntityTypeConfiguration<ChildClassEnrollment>
{
    public void Configure(EntityTypeBuilder<ChildClassEnrollment> b)
    {
        b.ToTable("ChildClassEnrollments", t => t.HasCheckConstraint("CK_ChildClassEnrollments_DateRange", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ChildId, x.StartDate }).HasDatabaseName("IX_ChildClassEnrollments_Child_StartDate");
        b.HasIndex(x => new { x.ClassroomId, x.StartDate }).HasDatabaseName("IX_ChildClassEnrollments_Classroom_StartDate");
        b.HasIndex(x => new { x.ChildId, x.ClassroomId, x.StartDate }).IsUnique().HasFilter("\"IsDeleted\" = 0").HasDatabaseName("UX_ChildClassEnrollments_Child_Class_StartDate");
    }
}

internal sealed class ClassTeacherAssignmentConfiguration : IEntityTypeConfiguration<ClassTeacherAssignment>
{
    public void Configure(EntityTypeBuilder<ClassTeacherAssignment> b)
    {
        b.ToTable("ClassTeacherAssignments", t => t.HasCheckConstraint("CK_ClassTeacherAssignments_DateRange", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.EmployeeId, x.StartDate }).HasDatabaseName("IX_ClassTeacherAssignments_Employee_StartDate");
        b.HasIndex(x => new { x.ClassroomId, x.StartDate }).HasDatabaseName("IX_ClassTeacherAssignments_Classroom_StartDate");
        b.HasIndex(x => new { x.EmployeeId, x.ClassroomId, x.StartDate }).IsUnique().HasFilter("\"IsDeleted\" = 0").HasDatabaseName("UX_ClassTeacherAssignments_Employee_Class_StartDate");
    }
}

internal sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> b)
    {
        b.ToTable("SubscriptionPlans", t => t.HasCheckConstraint("CK_SubscriptionPlans_DefaultFeeNonNegative", "CAST(\"DefaultFee\" AS REAL) >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired();
        b.Property(x => x.DefaultFee).HasPrecision(18, 2);
        b.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = 0").HasDatabaseName("UX_SubscriptionPlans_Name");
        b.HasMany(x => x.Subscriptions).WithOne(x => x.SubscriptionPlan).HasForeignKey(x => x.SubscriptionPlanId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("Subscriptions", t => {
            t.HasCheckConstraint("CK_Subscriptions_AmountDueNonNegative", "CAST(\"AmountDue\" AS REAL) >= 0");
            t.HasCheckConstraint("CK_Subscriptions_PeriodRange", "\"PeriodEnd\" >= \"PeriodStart\"");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.AmountDue).HasPrecision(18, 2);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.HasIndex(x => new { x.ChildId, x.PeriodStart }).HasDatabaseName("IX_Subscriptions_Child_PeriodStart");
        b.HasIndex(x => x.SubscriptionPlanId).HasDatabaseName("IX_Subscriptions_PlanId");
        b.HasMany(x => x.PaymentAllocations).WithOne(x => x.Subscription).HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InventoryCategoryConfiguration : IEntityTypeConfiguration<InventoryCategory>
{
    public void Configure(EntityTypeBuilder<InventoryCategory> b)
    {
        b.ToTable("InventoryCategories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired();
        b.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = 0").HasDatabaseName("UX_InventoryCategories_Name");
        b.HasMany(x => x.Items).WithOne(x => x.Category).HasForeignKey(x => x.CategoryId).IsRequired().OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> b)
    {
        b.ToTable("InventoryItems", t =>
        {
            t.HasCheckConstraint("CK_InventoryItems_UnitPriceNonNegative", "CAST(\"UnitPrice\" AS REAL) >= 0");
            t.HasCheckConstraint("CK_InventoryItems_QuantityNonNegative", "\"Quantity\" >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.Quantity).IsConcurrencyToken();
        b.HasIndex(x => new { x.CategoryId, x.Name }).HasDatabaseName("IX_InventoryItems_Category_Name");
        b.HasMany(x => x.StockMovements).WithOne(x => x.InventoryItem).HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.InvoiceLines).WithOne(x => x.InventoryItem).HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class FileAttachmentConfiguration : IEntityTypeConfiguration<FileAttachment>
{
    public void Configure(EntityTypeBuilder<FileAttachment> b)
    {
        b.ToTable("FileAttachments", t => t.HasCheckConstraint("CK_FileAttachments_SizeNonNegative", "\"SizeInBytes\" IS NULL OR \"SizeInBytes\" >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.OriginalFileName).IsRequired();
        b.Property(x => x.RelativePath).IsRequired();
        b.HasIndex(x => new { x.ChildId, x.Type }).HasDatabaseName("IX_FileAttachments_Child_Type");
        b.HasIndex(x => new { x.EmployeeId, x.Type }).HasDatabaseName("IX_FileAttachments_Employee_Type");
        b.HasOne(x => x.Child).WithMany(x => x.Attachments).HasForeignKey(x => x.ChildId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Employee).WithMany(x => x.Attachments).HasForeignKey(x => x.EmployeeId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments", t => t.HasCheckConstraint("CK_Payments_AmountNonNegative", "CAST(\"Amount\" AS REAL) >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.HasIndex(x => x.ChildId).HasDatabaseName("IX_Payments_ChildId");
        b.HasIndex(x => x.PaidAt).HasDatabaseName("IX_Payments_PaidAt");
        b.HasIndex(x => x.ReceiptNumber).IsUnique().HasDatabaseName("UX_Payments_ReceiptNumber");
        b.HasOne(x => x.Child).WithMany(x => x.Payments).HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Subscription).WithMany().HasForeignKey(x => x.SubscriptionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReceivedByUser).WithMany().HasForeignKey(x => x.ReceivedByUserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Allocations).WithOne(x => x.Payment).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> b)
    {
        b.ToTable("PaymentAllocations", t => t.HasCheckConstraint("CK_PaymentAllocations_AmountAppliedNonNegative", "CAST(\"AmountApplied\" AS REAL) >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.AmountApplied).HasPrecision(18, 2);
        b.HasIndex(x => x.PaymentId).HasDatabaseName("IX_PaymentAllocations_PaymentId");
        b.HasIndex(x => x.SubscriptionId).HasDatabaseName("IX_PaymentAllocations_SubscriptionId");
        b.HasOne(x => x.Payment).WithMany(x => x.Allocations).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Subscription).WithMany(x => x.PaymentAllocations).HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ChildAttendanceConfiguration : IEntityTypeConfiguration<ChildAttendance>
{
    public void Configure(EntityTypeBuilder<ChildAttendance> b)
    {
        b.ToTable("ChildAttendances");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.EntryMethod).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.HasIndex(x => new { x.ChildId, x.AttendanceDate }).IsUnique().HasFilter("\"IsDeleted\" = 0").HasDatabaseName("UX_ChildAttendances_Child_Date");
        b.HasOne(x => x.Child).WithMany(x => x.Attendances).HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class EmployeeAttendanceConfiguration : IEntityTypeConfiguration<EmployeeAttendance>
{
    public void Configure(EntityTypeBuilder<EmployeeAttendance> b)
    {
        b.ToTable("EmployeeAttendances");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.EntryMethod).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.HasIndex(x => new { x.EmployeeId, x.AttendanceDate }).IsUnique().HasFilter("\"IsDeleted\" = 0").HasDatabaseName("UX_EmployeeAttendances_Employee_Date");
        b.HasOne(x => x.Employee).WithMany(x => x.Attendances).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.ToTable("StockMovements");
        b.HasKey(x => x.Id);
        b.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.UnitCost).HasPrecision(18, 2);
        b.HasIndex(x => new { x.InventoryItemId, x.OccurredAt }).HasDatabaseName("IX_StockMovements_Item_OccurredAt");
        b.HasOne(x => x.InventoryItem).WithMany(x => x.StockMovements).HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.InvoiceLine).WithMany().HasForeignKey(x => x.InvoiceLineId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices", t => t.HasCheckConstraint("CK_Invoices_TotalAmountNonNegative", "CAST(\"TotalAmount\" AS REAL) >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.InvoiceNumber).IsRequired();
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.HasIndex(x => x.InvoiceNumber).IsUnique().HasDatabaseName("UX_Invoices_InvoiceNumber");
        b.HasIndex(x => x.InvoiceDate).HasDatabaseName("IX_Invoices_InvoiceDate");
        b.HasOne(x => x.Child).WithMany().HasForeignKey(x => x.ChildId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Lines).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> b)
    {
        b.ToTable("InvoiceLines", t => {
            t.HasCheckConstraint("CK_InvoiceLines_QuantityPositive", "\"Quantity\" > 0");
            t.HasCheckConstraint("CK_InvoiceLines_LineTotalNonNegative", "CAST(\"LineTotal\" AS REAL) >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.ItemNameSnapshot).IsRequired();
        b.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 2);
        b.Property(x => x.LineTotal).HasPrecision(18, 2);
        b.HasIndex(x => x.InvoiceId).HasDatabaseName("IX_InvoiceLines_InvoiceId");
        b.HasOne(x => x.Invoice).WithMany(x => x.Lines).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.InventoryItem).WithMany(x => x.InvoiceLines).HasForeignKey(x => x.InventoryItemId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> b)
    {
        b.ToTable("Expenses", t => t.HasCheckConstraint("CK_Expenses_AmountNonNegative", "CAST(\"Amount\" AS REAL) >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Category).IsRequired();
        b.Property(x => x.Description).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasIndex(x => x.ExpenseDate).HasDatabaseName("IX_Expenses_ExpenseDate");
        b.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class PayrollRecordConfiguration : IEntityTypeConfiguration<PayrollRecord>
{
    public void Configure(EntityTypeBuilder<PayrollRecord> b)
    {
        b.ToTable("PayrollRecords", t => {
            t.HasCheckConstraint("CK_PayrollRecords_PeriodRange", "\"PeriodEnd\" >= \"PeriodStart\"");
            t.HasCheckConstraint("CK_PayrollRecords_SalaryNonNegative", "CAST(\"AgreedSalary\" AS REAL) >= 0 AND CAST(\"AmountPaid\" AS REAL) >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.AgreedSalary).HasPrecision(18, 2);
        b.Property(x => x.Additions).HasPrecision(18, 2);
        b.Property(x => x.Deductions).HasPrecision(18, 2);
        b.Property(x => x.AmountPaid).HasPrecision(18, 2);
        b.Property(x => x.AdvanceDeduction).HasPrecision(18, 2);
        b.Property(x => x.InsuranceFunds).HasPrecision(18, 2);
        b.HasIndex(x => new { x.EmployeeId, x.PeriodStart }).HasDatabaseName("IX_PayrollRecords_Employee_PeriodStart");
        b.HasOne(x => x.Employee).WithMany(x => x.PayrollRecords).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EmployeeAdvanceConfiguration : IEntityTypeConfiguration<EmployeeAdvance>
{
    public void Configure(EntityTypeBuilder<EmployeeAdvance> b)
    {
        b.ToTable("EmployeeAdvances", t => {
            t.HasCheckConstraint("CK_EmployeeAdvances_Amounts", "CAST(\"OriginalAmount\" AS REAL) > 0 AND CAST(\"RemainingAmount\" AS REAL) >= 0 AND CAST(\"RemainingAmount\" AS REAL) <= CAST(\"OriginalAmount\" AS REAL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.OriginalAmount).HasPrecision(18, 2);
        b.Property(x => x.RemainingAmount).HasPrecision(18, 2);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.HasIndex(x => new { x.EmployeeId, x.AdvanceDate }).HasDatabaseName("IX_EmployeeAdvances_Employee_Date");
        b.HasOne(x => x.Employee).WithMany(x => x.Advances).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EmployeeAdvanceRepaymentConfiguration : IEntityTypeConfiguration<EmployeeAdvanceRepayment>
{
    public void Configure(EntityTypeBuilder<EmployeeAdvanceRepayment> b)
    {
        b.ToTable("EmployeeAdvanceRepayments", t => t.HasCheckConstraint("CK_EmployeeAdvanceRepayments_Amount", "CAST(\"Amount\" AS REAL) > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasIndex(x => new { x.EmployeeAdvanceId, x.PayrollRecordId }).IsUnique();
        b.HasOne(x => x.EmployeeAdvance).WithMany(x => x.Repayments).HasForeignKey(x => x.EmployeeAdvanceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PayrollRecord).WithMany(x => x.AdvanceRepayments).HasForeignKey(x => x.PayrollRecordId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).IsRequired();
        b.Property(x => x.EntityType).IsRequired();
        b.HasIndex(x => x.Timestamp).HasDatabaseName("IX_AuditLogs_Timestamp");
        b.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("IX_AuditLogs_EntityType_EntityId");
        b.HasOne(x => x.UserAccount).WithMany().HasForeignKey(x => x.UserAccountId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> b)
    {
        b.ToTable("AppSettings");
        b.HasKey(x => x.Key);
        b.Property(x => x.Value).IsRequired();
    }
}

internal sealed class AttendanceDeviceConfiguration : IEntityTypeConfiguration<AttendanceDevice>
{
    public void Configure(EntityTypeBuilder<AttendanceDevice> b)
    {
        b.ToTable("AttendanceDevices");
        b.HasKey(x => x.Id);
        b.Property(x => x.DeviceType).IsRequired();
        b.Property(x => x.DisplayName).IsRequired();
        b.HasMany(x => x.Events).WithOne(x => x.AttendanceDevice).HasForeignKey(x => x.AttendanceDeviceId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DeviceAttendanceEventConfiguration : IEntityTypeConfiguration<DeviceAttendanceEvent>
{
    public void Configure(EntityTypeBuilder<DeviceAttendanceEvent> b)
    {
        b.ToTable("DeviceAttendanceEvents");
        b.HasKey(x => x.Id);
        b.Property(x => x.DeviceEventIdentifier).IsRequired();
        b.HasIndex(x => new { x.AttendanceDeviceId, x.DeviceEventIdentifier }).IsUnique().HasDatabaseName("UX_DeviceAttendanceEvents_Device_Identifier");
        b.HasOne(x => x.AttendanceDevice).WithMany(x => x.Events).HasForeignKey(x => x.AttendanceDeviceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Child).WithMany().HasForeignKey(x => x.ChildId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class ManualIncomeConfiguration : IEntityTypeConfiguration<ManualIncome>
{
    public void Configure(EntityTypeBuilder<ManualIncome> b)
    {
        b.ToTable("ManualIncomes", t => t.HasCheckConstraint("CK_ManualIncomes_AmountPositive", "CAST(\"Amount\" AS REAL) > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Source).IsRequired();
        b.Property(x => x.Description).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
        b.HasIndex(x => x.IncomeDate).HasDatabaseName("IX_ManualIncomes_IncomeDate");
        b.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}

