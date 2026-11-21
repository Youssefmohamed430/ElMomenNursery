namespace ElMomenNursery.Application.DTOs;

public sealed class UpdateExpenseDto
{
    public DateOnly ExpenseDate { get; set; }
    public required string Category { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
}

