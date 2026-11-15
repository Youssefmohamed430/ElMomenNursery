using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Mappings;
using ElMomenNursery.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ElMomenNursery.Application.Services;

public class EmployeeService(IUnitOfWork unitOfWork, ILogger<EmployeeService> logger) : IEmployeeService
{
    public async Task<EmployeeDto> CreateEmployee(CreateEmployeeDto employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ValidateRequiredFields(employee.FullName, employee.NationalIdNumber, employee.Address, employee.Phone, employee.JobDesignation);
        if (employee.Salary < 0)
            throw new ArgumentOutOfRangeException(nameof(employee), "Salary cannot be negative.");
        employee.NationalIdNumber = employee.NationalIdNumber.Trim();
        if (await unitOfWork.Repository<Employee>().AnyAsync(e => e.NationalIdNumber == employee.NationalIdNumber))
        {
            logger.LogWarning("Employee creation rejected because national ID is already registered");
            throw new InvalidOperationException("An employee with this national ID already exists.");
        }

        var newEmp = employee.Adapt<Employee>();
        await unitOfWork.Repository<Employee>().AddAsync(newEmp);
        var affectedRows = await unitOfWork.SaveChangesAsync();
        if (affectedRows == 0)
            logger.LogWarning("Employee creation saved no rows; employeeId={EmployeeId}", newEmp.Id);
        logger.LogInformation("Created employee record; employeeId={EmployeeId}", newEmp.Id);
        return newEmp.Adapt<EmployeeDto>();
    }

    public async Task<bool> DeleteEmployee(Guid id)
    {
        var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null) return false;

        unitOfWork.Repository<Employee>().Remove(employee);
        var affectedRows = await unitOfWork.SaveChangesAsync();
        if (affectedRows == 0)
            logger.LogWarning("Employee deletion saved no rows; employeeId={EmployeeId}", id);
        logger.LogInformation("Deleted employee record; employeeId={EmployeeId}", id);
        return true;
    }

    public async Task<EmployeeDto> GetEmployeeById(Guid id)
    {
        var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null) throw new KeyNotFoundException($"Employee '{id}' was not found.");
        return employee.Adapt<EmployeeDto>();
    }

    public async Task<EmployeeDto> GetEmployeeByName(string FullName)
    {
         var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.FullName == FullName);
        if (employee is null) throw new KeyNotFoundException($"Employee '{FullName}' was not found.");
        return employee.Adapt<EmployeeDto>();
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployees()
    {
        var employees = await unitOfWork.Repository<Employee>().ListAsync();
        return employees.Adapt<IEnumerable<EmployeeDto>>();
    }

    public async Task<EmployeeDto> UpdateEmployee(Guid id, UpdateEmployeeDto employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        var existingEmployee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.Id == id);
        if (existingEmployee == null)
        {
            throw new KeyNotFoundException($"Employee '{id}' was not found.");
        }

        ValidateSuppliedRequiredField(employee.FullName, nameof(employee.FullName));
        ValidateSuppliedRequiredField(employee.NationalIdNumber, nameof(employee.NationalIdNumber));
        ValidateSuppliedRequiredField(employee.Address, nameof(employee.Address));
        ValidateSuppliedRequiredField(employee.Phone, nameof(employee.Phone));
        ValidateSuppliedRequiredField(employee.JobDesignation, nameof(employee.JobDesignation));
        if (employee.Salary < 0)
            throw new ArgumentOutOfRangeException(nameof(employee), "Salary cannot be negative.");
        if (employee.NationalIdNumber is not null && await unitOfWork.Repository<Employee>().AnyAsync(e =>
                e.Id != id && e.NationalIdNumber == employee.NationalIdNumber.Trim()))
        {
            logger.LogWarning("Employee update rejected because national ID is already registered; employeeId={EmployeeId}", id);
            throw new InvalidOperationException("An employee with this national ID already exists.");
        }

        if (employee.NationalIdNumber is not null)
            employee.NationalIdNumber = employee.NationalIdNumber.Trim();
        employee.Adapt(existingEmployee, MapsterConfigForEmployee.Config);
        existingEmployee.UpdatedAt = DateTimeOffset.UtcNow;
        unitOfWork.Repository<Employee>().Update(existingEmployee);
        var affectedRows = await unitOfWork.SaveChangesAsync();
        if (affectedRows == 0)
            logger.LogWarning("Employee update saved no rows; employeeId={EmployeeId}", id);
        logger.LogInformation("Updated employee record; employeeId={EmployeeId}", id);
        return existingEmployee.Adapt<EmployeeDto>();
    }

    private static void ValidateRequiredFields(params string[] fields)
    {
        if (fields.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Employee name, national ID, address, phone, and job designation are required.");
    }

    private static void ValidateSuppliedRequiredField(string? value, string propertyName)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{propertyName} cannot be empty.", propertyName);
    }
}
