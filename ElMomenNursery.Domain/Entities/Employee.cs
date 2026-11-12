namespace ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;

public sealed class Employee
{
    public Guid Id { get; init; } = Guid.NewGuid();
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
    public decimal Salary { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public UserAccount? UserAccount { get; set; }
    public ICollection<ClassTeacherAssignment> TeachingAssignments { get; } = new List<ClassTeacherAssignment>();
    public ICollection<FileAttachment> Attachments { get; } = new List<FileAttachment>();
    public ICollection<EmployeeAttendance> Attendances { get; } = new List<EmployeeAttendance>();
    public ICollection<PayrollRecord> PayrollRecords { get; } = new List<PayrollRecord>();
}
