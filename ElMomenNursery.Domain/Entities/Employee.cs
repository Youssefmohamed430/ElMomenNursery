namespace ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;

public sealed class Employee : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public required string FullName { get; set; }
    public string? Qualification { get; set; }
    public int? GraduationYear { get; set; }
    public bool HasExperience { get; set; }
    public required string NationalIdNumber { get; set; }
    public required string Address { get; set; }
    public MaritalStatus MaritalStatus { get; set; }
    public required string Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public required string JobDesignation { get; set; }
    public DateOnly HireDate { get; set; }
    public string ShiftStartTime { get; set; } = "07:30";
    public decimal Salary { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public UserAccount? UserAccount { get; set; }
    public ICollection<ClassTeacherAssignment> TeachingAssignments { get; } = new List<ClassTeacherAssignment>();
    public ICollection<FileAttachment> Attachments { get; } = new List<FileAttachment>();
    public ICollection<EmployeeAttendance> Attendances { get; } = new List<EmployeeAttendance>();
    public ICollection<PayrollRecord> PayrollRecords { get; } = new List<PayrollRecord>();
    public ICollection<EmployeeAdvance> Advances { get; } = new List<EmployeeAdvance>();
}
