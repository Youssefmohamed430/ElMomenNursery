namespace ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;

public sealed class Child : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public required string FullName { get; set; }
    public required string Address { get; set; }
    public DateOnly EnrollmentDate { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public ChildGender Gender { get; set; }
    public required string FatherName { get; set; }
    public string? FatherOccupation { get; set; }
    public required string FatherPhone { get; set; }
    public string? FatherWhatsApp { get; set; }
    public required string MotherName { get; set; }
    public string? MotherOccupation { get; set; }
    public required string MotherPhone { get; set; }
    public string? MotherWhatsApp { get; set; }
    public string? Notes { get; set; }
    public bool HasMedicalConditions { get; set; }
    public bool PreviouslyAttendedNursery { get; set; }
    public Shift Shift { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<Subscription> Subscriptions { get; } = new List<Subscription>();
    public ICollection<ChildClassEnrollment> ClassEnrollments { get; } = new List<ChildClassEnrollment>();
    public ICollection<FileAttachment> Attachments { get; } = new List<FileAttachment>();
    public ICollection<Payment> Payments { get; } = new List<Payment>();
    public ICollection<ChildAttendance> Attendances { get; } = new List<ChildAttendance>();
}

public enum Shift {
    Morning,
    Afternoon,
    FullDay,
    PeriodAndAHalf
}
