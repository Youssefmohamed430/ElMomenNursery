namespace ElMomenNursery.Application.DTOs;

public sealed class CreateEmployeeAdvanceDto
{
    public Guid EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly AdvanceDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class EmployeeAdvanceDto
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public decimal OriginalAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public DateOnly AdvanceDate { get; init; }
    public string? Notes { get; init; }
}
