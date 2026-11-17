using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IEmployeeService
{
    Task<Result<IReadOnlyList<EmployeeDto>>> GetEmployees();
    Task<Result<IReadOnlyList<EmployeeDto>>> GetDeletedEmployees(CancellationToken cancellationToken = default);
    Task<Result> RestoreEmployee(Guid id, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDto>> GetEmployeeById(Guid id);
    Task<Result<EmployeeDto>> GetEmployeeByName(string fullName);
    Task<Result<EmployeeDto>> CreateEmployee(CreateEmployeeDto employee);
    Task<Result<EmployeeDto>> UpdateEmployee(Guid id, UpdateEmployeeDto employee);
    Task<Result> DeleteEmployee(Guid id);
}
