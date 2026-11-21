namespace ElMomenNursery.Application.DTOs;

public sealed class CreateManualIncomeDto
{
    public DateOnly IncomeDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public required string Source { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

