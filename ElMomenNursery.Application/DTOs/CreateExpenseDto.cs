namespace ElMomenNursery.Application.DTOs;

public sealed class CreateExpenseDto
{
    public DateOnly ExpenseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public required string Category { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
}

