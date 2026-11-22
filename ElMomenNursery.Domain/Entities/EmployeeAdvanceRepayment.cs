namespace ElMomenNursery.Domain.Entities;

public sealed class EmployeeAdvanceRepayment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmployeeAdvanceId { get; set; }
    public required EmployeeAdvance EmployeeAdvance { get; set; }
    public Guid PayrollRecordId { get; set; }
    public required PayrollRecord PayrollRecord { get; set; }
    public decimal Amount { get; set; }
    public DateOnly RepaymentDate { get; set; }
}
