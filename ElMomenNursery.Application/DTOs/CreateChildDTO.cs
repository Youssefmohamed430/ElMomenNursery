using ElMomenNursery.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElMomenNursery.Application.DTOs;

public class CreateChildDTO
{
    [Required(ErrorMessage = "اسم الطفل مطلوب.")]
    [StringLength(100, ErrorMessage = "يجب ألا يزيد اسم الطفل عن 100 حرف.")]
    public required string FullName { get; set; }

    [Required(ErrorMessage = "العنوان مطلوب.")]
    [StringLength(200, ErrorMessage = "يجب ألا يزيد العنوان عن 200 حرف.")]
    public required string Address { get; set; }

    [Required(ErrorMessage = "تاريخ الالتحاق مطلوب.")]
    [DataType(DataType.Date, ErrorMessage = "أدخل تاريخ التحاق صحيحًا.")]
    //[CustomValidation(typeof(CreateChildDTO), nameof(ValidateEnrollmentDate))]
    [Range(typeof(DateOnly), "2000-01-01", "2100-12-31", ErrorMessage = "يجب أن يكون تاريخ الالتحاق بين 01/01/2000 و31/12/2100.")]
    public DateOnly EnrollmentDate { get; set; }

    [Required(ErrorMessage = "تاريخ الميلاد مطلوب.")]
    [DataType(DataType.Date, ErrorMessage = "أدخل تاريخ ميلاد صحيحًا.")]
    [Range(typeof(DateOnly), "2000-01-01", "2100-12-31", ErrorMessage = "يجب أن يكون تاريخ الميلاد بين 01/01/2000 و31/12/2100.")]
    public DateOnly DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    public ChildGender Gender { get; set; }

    [Required(ErrorMessage = "FatherName is required.")]
    public required string FatherName { get; set; }

    public string? FatherOccupation { get; set; }

    [Required(ErrorMessage = "FatherPhone is required.")]
    public required string FatherPhone { get; set; }

    public string? FatherWhatsApp { get; set; }

    [Required(ErrorMessage = "MotherName is required.")]
    public required string MotherName { get; set; }

    public string? MotherOccupation { get; set; }

    [Required(ErrorMessage = "MotherPhone is required.")]   
    public required string MotherPhone { get; set; }

    public string? MotherWhatsApp { get; set; }

    public string? Notes { get; set; }

    public bool HasMedicalConditions { get; set; }

    public bool PreviouslyAttendedNursery { get; set; }
}
