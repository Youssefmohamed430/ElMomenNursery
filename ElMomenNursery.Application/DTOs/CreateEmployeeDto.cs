using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public class CreateEmployeeDto
{
    public required string FullName { get; set; }
    public string? Qualification { get; set; }
    public int? GraduationYear { get; set; }
    public required bool HasExperience { get; set; }
    public required string NationalIdNumber { get; set; }
    public required string Address { get; set; }
    public required MaritalStatus MaritalStatus { get; set; }
    public required string Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public required string JobDesignation { get; set; }
    public required DateOnly HireDate { get; set; }
    public required decimal Salary { get; set; }
}
