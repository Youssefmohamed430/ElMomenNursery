using ElMomenNursery.Domain.Entities;

namespace ElMomenNursery.Application.DTOs
{
    public class CreatePayrollRecordDto
    {
        public Guid EmployeeId { get; set; }
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public decimal AgreedSalary { get; set; }
        public decimal? Additions { get; set; }
        public decimal? Deductions { get; set; }
        public decimal? AmountPaid { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public bool IsPaid { get; set; } = false;
        public string? Notes { get; set; }
        public decimal Insurancefunds { get; set; }
    }
}
