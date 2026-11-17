using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IEmployeeAttendanceService
{
    Task<Result<IReadOnlyList<EmployeeDailyAttendanceDto>>> GetDailyAttendanceAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EmployeeAttendanceDto>>> GetEmployeeAttendanceByRangeAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AttendanceEmployeeOptionDto>>> GetEmployeesForHistorySelectionAsync(CancellationToken cancellationToken = default);
    Task<Result<EmployeeAttendanceDto>> MarkManualAttendanceAsync(
        CreateEmployeeAttendanceDto employeeAttendanceDto,
        CancellationToken cancellationToken = default);
    Task<Result<EmployeeAttendanceDto>> UpdateEmployeeAttendanceAsync(
        Guid id,
        UpdateEmployeeAttendanceDto employeeAttendanceDto,
        CancellationToken cancellationToken = default);
    Task<Result<EmployeeAttendanceDto>> RecordCheckOutAsync(Guid employeeId, DateOnly date, DateTimeOffset checkOutTime, CancellationToken cancellationToken = default);
}
