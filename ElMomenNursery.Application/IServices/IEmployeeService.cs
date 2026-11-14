using ElMomenNursery.Application.DTOs;

namespace ElMomenNursery.Application.IServices;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetEmployees();
    Task<EmployeeDto> GetEmployeeById(Guid id);
    Task<EmployeeDto> GetEmployeeByName(string FullName);
    Task<EmployeeDto> CreateEmployee(CreateEmployeeDto employee);
    Task<EmployeeDto> UpdateEmployee(Guid id, UpdateEmployeeDto employee);
    Task<bool> DeleteEmployee(Guid id);
}
