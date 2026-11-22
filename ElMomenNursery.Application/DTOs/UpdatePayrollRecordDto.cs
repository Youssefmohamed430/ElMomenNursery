namespace ElMomenNursery.Application.DTOs;

public class UpdatePayrollRecordDto
{
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal AgreedSalary { get; set; }
    public decimal Additions { get; set; }
    public decimal Deductions { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public string? Notes { get; set; }
    public decimal Insurancefunds { get; set; }
}
