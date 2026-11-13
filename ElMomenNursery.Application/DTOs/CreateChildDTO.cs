using ElMomenNursery.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElMomenNursery.Application.DTOs;

public class CreateChildDTO
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public required string FullName { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    public required string Address { get; set; }

    [Required(ErrorMessage = "EnrollmentDate is required.")]
    [DataType(DataType.Date, ErrorMessage = "EnrollmentDate must be a valid date.")]
    //[CustomValidation(typeof(CreateChildDTO), nameof(ValidateEnrollmentDate))]
    [Range(typeof(DateOnly), "2020-01-01", "2100-12-31", ErrorMessage = "EnrollmentDate must be between 2000-01-01 and 2100-12-31.")]
    public DateOnly EnrollmentDate { get; set; }

    [Required(ErrorMessage = "DateOfBirth is required.")]
    [DataType(DataType.Date, ErrorMessage = "DateOfBirth must be a valid date.")]
    [Range(typeof(DateOnly), "2000-01-01", "2100-12-31", ErrorMessage = "DateOfBirth must be between 2000-01-01 and 2100-12-31.")]
    public DateOnly DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    public ChildGender Gender { get; set; }

    [Required(ErrorMessage = "FatherName is required.")]
    public required string FatherName { get; set; }

    [Required(ErrorMessage = "FatherOccupation is required.")]
    public string? FatherOccupation { get; set; }

    [Required(ErrorMessage = "FatherPhone is required.")]
    public required string FatherPhone { get; set; }

    [Required(ErrorMessage = "FatherWhatsApp is required.")]
    public string? FatherWhatsApp { get; set; }

    [Required(ErrorMessage = "MotherName is required.")]
    public required string MotherName { get; set; }

    [Required(ErrorMessage = "MotherOccupation is required.")]
    public string? MotherOccupation { get; set; }

    [Required(ErrorMessage = "MotherPhone is required.")]   
    public required string MotherPhone { get; set; }

    [Required(ErrorMessage = "MotherWhatsApp is required.")]
    public string? MotherWhatsApp { get; set; }

    [Required(ErrorMessage = "Notes are required.")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "HasMedicalConditions is required.")]
    public bool HasMedicalConditions { get; set; }

    [Required(ErrorMessage = "PreviouslyAttendedNursery is required.")]
    public bool PreviouslyAttendedNursery { get; set; }
}
