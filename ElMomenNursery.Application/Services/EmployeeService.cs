using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Mappings;
using ElMomenNursery.Domain.Entities;

namespace ElMomenNursery.Application.Services;

public class EmployeeService(IUnitOfWork unitOfWork) : IEmployeeService
{
    public async Task<EmployeeDto> CreateEmployee(CreateEmployeeDto employee)
    {
        var newEmp = employee.Adapt<Employee>();
        await unitOfWork.Repository<Employee>().AddAsync(newEmp);
        await unitOfWork.SaveChangesAsync();
        return newEmp.Adapt<EmployeeDto>();
    }

    public async Task<bool> DeleteEmployee(Guid id)
    {
        var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null) return false;

        unitOfWork.Repository<Employee>().Remove(employee);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<EmployeeDto> GetEmployeeById(Guid id)
    {
        var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.Id == id);
        return employee.Adapt<EmployeeDto>();
    }

    public async Task<EmployeeDto> GetEmployeeByName(string FullName)
    {
         var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.FullName == FullName);
        return employee.Adapt<EmployeeDto>();
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployees()
    {
        var employees = await unitOfWork.Repository<Employee>().ListAsync();
        return employees.Adapt<IEnumerable<EmployeeDto>>();
    }

    public async Task<EmployeeDto> UpdateEmployee(Guid id, UpdateEmployeeDto employee)
    {
        var existingEmployee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.Id == id);
        if (existingEmployee == null)
        {
            throw new Exception("Employee not found");
        }
        employee.Adapt(existingEmployee, MapsterConfigForEmployee.Config);
        unitOfWork.Repository<Employee>().Update(existingEmployee);
        await unitOfWork.SaveChangesAsync();
        return existingEmployee.Adapt<EmployeeDto>();
    }
}
