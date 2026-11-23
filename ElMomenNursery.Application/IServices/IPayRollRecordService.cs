using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IPayRollRecordService
{
    Task<Result<IReadOnlyList<PayrollRecordDto>>> GetAllPayrollRecordsAsync();
    Task<Result<IReadOnlyList<PayrollRecordDto>>> GetPayrollRecordsByDateAsync(DateOnly startDate, DateOnly endDate);
    Task<Result<IReadOnlyList<PayrollRecordDto>>> GetPayrollRecordsByEmployeeNameAsync(string employeeName);
    Task<Result<IReadOnlyList<PayrollRecordDto>>> GetPayrollRecordsByEmployeeNameByRangeAsync(string employeeName, DateOnly startDate, DateOnly endDate);
    Task<Result<PayrollRecordDto>> CreatePayRollRecord(CreatePayrollRecordDto payrollRecordDto);
    Task<Result<PayrollBatchResultDto>> CreatePayrollRecordsForAllEmployeesAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<Result<PayrollRecordDto>> UpdatePayRollRecordAsync(Guid payrollRecordId, UpdatePayrollRecordDto payrollRecordDto);
    Task<Result> DeletePayRollRecordAsync(Guid payrollRecordId);
    Task<Result<IReadOnlyList<PayrollRecordDto>>> GetDeletedPayrollRecordsAsync(CancellationToken cancellationToken = default);
    Task<Result<PayrollRecordDto>> MarkRecordAsPaid(Guid payrollRecordId);
    Task<Result<PayrollRecordDto>> CalculateFinalSalary(Guid payrollRecordId);
    Task<Result<EmployeeAdvanceDto>> CreateEmployeeAdvanceAsync(CreateEmployeeAdvanceDto dto, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EmployeeAdvanceDto>>> GetEmployeeAdvancesAsync(Guid? employeeId = null, CancellationToken cancellationToken = default);
}
