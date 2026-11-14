using ElMomenNursery.Application.DTOs;

namespace ElMomenNursery.Application.IServices;

public interface IEmployeeAttendanceService
{
    Task<IReadOnlyList<EmployeeDailyAttendanceDto>> GetDailyAttendanceAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeAttendanceDto>> GetEmployeeAttendanceByRangeAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
    Task<EmployeeAttendanceDto> MarkManualAttendanceAsync(
        CreateEmployeeAttendanceDto employeeAttendanceDto,
        CancellationToken cancellationToken = default);
    Task<EmployeeAttendanceDto> UpdateEmployeeAttendanceAsync(
        Guid id,
        UpdateEmployeeAttendanceDto employeeAttendanceDto,
        CancellationToken cancellationToken = default);
}
