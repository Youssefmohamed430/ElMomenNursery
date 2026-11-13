using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public class ChildDto
{
    public Guid Id { get; init; }
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
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
