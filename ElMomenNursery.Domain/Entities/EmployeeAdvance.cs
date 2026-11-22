namespace ElMomenNursery.Domain.Entities;

public sealed class EmployeeAdvance
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public required Employee Employee { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateOnly AdvanceDate { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public ICollection<EmployeeAdvanceRepayment> Repayments { get; } = new List<EmployeeAdvanceRepayment>();
}
