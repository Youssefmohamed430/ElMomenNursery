using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public sealed class FinanceTransactionDto
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public FinanceTransactionType Type { get; init; }
    public string TypeLabel { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Reference { get; init; }
    public string? RecordedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public bool IsEditable { get; init; }
}

