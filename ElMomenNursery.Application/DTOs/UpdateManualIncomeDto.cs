namespace ElMomenNursery.Application.DTOs;

public sealed class UpdateManualIncomeDto
{
    public DateOnly IncomeDate { get; set; }
    public required string Source { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    public string? PayerName { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

