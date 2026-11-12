namespace ElMomenNursery.Domain.Entities;

public sealed class PayrollRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal AgreedSalary { get; set; }
    public decimal Additions { get; set; }
    public decimal Deductions { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public bool IsPaid { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public required Employee Employee { get; set; }
}
