using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public class UpdateEmployeeDto
{
    public string? FullName { get; set; }
    public string? Qualification { get; set; }
    public int? GraduationYear { get; set; }
    public bool? HasExperience { get; set; }
    public string? NationalIdNumber { get; set; }
    public string? Address { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? JobDesignation { get; set; }
    public DateOnly? HireDate { get; set; }
    public string? ShiftStartTime { get; set; }
    public decimal? Salary { get; set; }
    public decimal? InsuranceFunds { get; set; }
}
