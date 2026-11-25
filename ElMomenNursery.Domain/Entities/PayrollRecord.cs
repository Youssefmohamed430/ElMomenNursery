namespace ElMomenNursery.Domain.Entities;

public sealed class PayrollRecord : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal AgreedSalary { get; set; }
    public decimal Additions { get; set; }
    public decimal Deductions { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public bool IsPaid { get; set; }
    public bool IsCalculated { get; set; }
    public string? Notes { get; set; }
    public decimal InsuranceFunds { get; set; }
    public decimal AdvanceDeduction { get; set; }
    public int LateMinutes { get; set; }
    public decimal LateDeduction { get; set; }
    public decimal ProratedSalary { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public required Employee Employee { get; set; }
    public ICollection<EmployeeAdvanceRepayment> AdvanceRepayments { get; } = new List<EmployeeAdvanceRepayment>();
}
