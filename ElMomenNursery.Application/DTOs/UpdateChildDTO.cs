using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public class UpdateChildDTO
{
    public string FullName { get; set; }
    public string Address { get; set; }
    public DateOnly EnrollmentDate { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public ChildGender Gender { get; set; }
    public string FatherName { get; set; }
    public string FatherOccupation { get; set; }
    public string FatherPhone { get; set; }
    public string FatherWhatsApp { get; set; }
    public string MotherName { get; set; }
    public string MotherOccupation { get; set; }
    public string MotherPhone { get; set; }
    public string MotherWhatsApp { get; set; }
    public string Notes { get; set; }
    public bool HasMedicalConditions { get; set; }
    public bool PreviouslyAttendedNursery { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
