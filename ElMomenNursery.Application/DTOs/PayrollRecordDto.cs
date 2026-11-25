namespace ElMomenNursery.Application.DTOs;

public sealed record PayrollBatchResultDto(int CreatedCount, int SkippedCount);

public class PayrollRecordDto
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal AgreedSalary { get; set; }
    public decimal DailySalary { get; set; }
    public int AbsenceDays { get; set; }
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
}
