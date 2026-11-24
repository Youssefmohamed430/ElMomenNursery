
namespace ElMomenNursery.Application.Services;

public sealed class EmployeeService(IUnitOfWork unitOfWork, ILogger<EmployeeService> logger, IPermissionService permissions) : IEmployeeService
{
    public async Task<Result<EmployeeDto>> CreateEmployee(CreateEmployeeDto employee)
    {
        var permissionError = PermissionGuard.Check(permissions, "Employees.Manage", EmployeeErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        if (employee is null || HasMissingRequiredField(employee.FullName, employee.NationalIdNumber, employee.Address, employee.Phone, employee.JobDesignation))
            return EmployeeErrors.InvalidData;
        if (employee.Salary < 0) return EmployeeErrors.InvalidSalary;
        if (!ValidNationalId(employee.NationalIdNumber)) return EmployeeErrors.InvalidNationalId;
        if (!PhoneNumberValidator.IsValidEgyptianMobile(employee.Phone)
            || !string.IsNullOrWhiteSpace(employee.AlternatePhone) && !PhoneNumberValidator.IsValidEgyptianMobile(employee.AlternatePhone)) return EmployeeErrors.InvalidPhone;
        if (employee.GraduationYear is int graduationYear && (graduationYear < 1900 || graduationYear > DateTime.Today.Year)) return EmployeeErrors.InvalidGraduationYear;

        employee.NationalIdNumber = employee.NationalIdNumber.Trim();
        // The database's unique index spans soft-deleted rows too, so include them
        // in the friendly duplicate check instead of letting SQLite reject the save.
        if (await unitOfWork.Repository<Employee>().Query().IgnoreQueryFilters()
                .AnyAsync(e => e.NationalIdNumber == employee.NationalIdNumber))
            return EmployeeErrors.DuplicateNationalId;

        var entity = employee.Adapt<Employee>();
        await unitOfWork.Repository<Employee>().AddAsync(entity);
        try
        {
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Created employee record; employeeId={EmployeeId}", entity.Id);
            return Result.Success(entity.Adapt<EmployeeDto>(), EmployeeErrors.Created.Description);
        }
        catch (DbUpdateException exception)
        {
            var error = PersistenceErrorMapper.FromException(exception);
            if (error == PersistenceErrors.Unique) return EmployeeErrors.DuplicateNationalId;
            logger.LogWarning(exception, "Employee creation failed; errorCode={ErrorCode}", error.Code);
            return error;
        }
    }

    public async Task<Result> DeleteEmployee(Guid id)
    {
        var permissionError = PermissionGuard.Check(permissions, "Employees.Manage", EmployeeErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null) return EmployeeErrors.NotFound;
        unitOfWork.Repository<Employee>().Remove(employee);
        try
        {
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Deleted employee record; employeeId={EmployeeId}", id);
            return Result.Success(EmployeeErrors.Deleted.Description);
        }
        catch (DbUpdateException exception)
        {
            var error = PersistenceErrorMapper.FromException(exception);
            logger.LogWarning(exception, "Employee deletion failed; employeeId={EmployeeId}; errorCode={ErrorCode}", id, error.Code);
            return error;
        }
    }

    public async Task<Result<EmployeeDto>> GetEmployeeById(Guid id)
    {
        var permissionError = PermissionGuard.Check(permissions, "Employees.View", EmployeeErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.Id == id);
        return employee is null ? EmployeeErrors.NotFound : Result.Success(employee.Adapt<EmployeeDto>());
    }

    public async Task<Result<EmployeeDto>> GetEmployeeByName(string fullName)
    {
        var permissionError = PermissionGuard.Check(permissions, "Employees.View", EmployeeErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        if (string.IsNullOrWhiteSpace(fullName)) return EmployeeErrors.InvalidData;
        var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.FullName == fullName.Trim());
        return employee is null ? EmployeeErrors.NotFound : Result.Success(employee.Adapt<EmployeeDto>());
    }

    public async Task<Result<IReadOnlyList<EmployeeDto>>> GetEmployees()
    {
        var permissionError = PermissionGuard.Check(permissions, "Employees.View", EmployeeErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var employees = await unitOfWork.Repository<Employee>().ListAsync();
        return Result.Success<IReadOnlyList<EmployeeDto>>(employees.Adapt<List<EmployeeDto>>());
    }

    public async Task<Result<IReadOnlyList<EmployeeDto>>> GetDeletedEmployees(CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Employees.Restore", EmployeeErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var employees = await unitOfWork.Repository<Employee>().Query().IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<EmployeeDto>>(employees.Adapt<List<EmployeeDto>>());
    }

    public async Task<Result> RestoreEmployee(Guid id, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Employees.Restore", EmployeeErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var employees = unitOfWork.Repository<Employee>();
        var employee = await employees.Query(asNoTracking: false).IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id && x.IsDeleted, cancellationToken);
        if (employee is null) return EmployeeErrors.NotFound;
        if (await employees.AnyAsync(x => x.NationalIdNumber == employee.NationalIdNumber, cancellationToken)) return EmployeeErrors.DuplicateNationalId;
        employee.IsDeleted = false; employee.DeletedAt = null; employee.DeletedByUserId = null; employee.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(EmployeeErrors.Restored.Description);
        }
        catch (DbUpdateException exception)
        {
            var error = PersistenceErrorMapper.FromException(exception);
            return error == PersistenceErrors.Unique ? EmployeeErrors.DuplicateNationalId : error;
        }
    }

    public async Task<Result<EmployeeDto>> UpdateEmployee(Guid id, UpdateEmployeeDto employee)
    {
        var permissionError = PermissionGuard.Check(permissions, "Employees.Manage", EmployeeErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        if (employee is null) return EmployeeErrors.InvalidData;
        var existing = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(e => e.Id == id, asNoTracking: false);
        if (existing is null) return EmployeeErrors.NotFound;

        if (new[] { employee.FullName, employee.NationalIdNumber, employee.Address, employee.Phone, employee.JobDesignation }
            .Any(value => value is not null && string.IsNullOrWhiteSpace(value))) return EmployeeErrors.InvalidData;
        if (employee.Salary < 0) return EmployeeErrors.InvalidSalary;
        if (employee.NationalIdNumber is not null && !ValidNationalId(employee.NationalIdNumber)) return EmployeeErrors.InvalidNationalId;
        if (employee.Phone is not null && !PhoneNumberValidator.IsValidEgyptianMobile(employee.Phone)) return EmployeeErrors.InvalidPhone;
        if (!string.IsNullOrWhiteSpace(employee.AlternatePhone) && !PhoneNumberValidator.IsValidEgyptianMobile(employee.AlternatePhone)) return EmployeeErrors.InvalidPhone;
        if (employee.GraduationYear is int graduationYear && (graduationYear < 1900 || graduationYear > DateTime.Today.Year)) return EmployeeErrors.InvalidGraduationYear;
        if (employee.NationalIdNumber is not null && await unitOfWork.Repository<Employee>().Query().IgnoreQueryFilters().AnyAsync(e =>
                e.Id != id && e.NationalIdNumber == employee.NationalIdNumber.Trim())) return EmployeeErrors.DuplicateNationalId;

        if (employee.NationalIdNumber is not null) employee.NationalIdNumber = employee.NationalIdNumber.Trim();
        employee.Adapt(existing, MapsterConfigForEmployee.Config);
        existing.Qualification = string.IsNullOrWhiteSpace(employee.Qualification) ? null : employee.Qualification.Trim();
        existing.AlternatePhone = string.IsNullOrWhiteSpace(employee.AlternatePhone) ? null : employee.AlternatePhone.Trim();
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Updated employee record; employeeId={EmployeeId}", id);
            return Result.Success(existing.Adapt<EmployeeDto>(), EmployeeErrors.Updated.Description);
        }
        catch (DbUpdateException exception)
        {
            var error = PersistenceErrorMapper.FromException(exception);
            if (error == PersistenceErrors.Unique) return EmployeeErrors.DuplicateNationalId;
            logger.LogWarning(exception, "Employee update failed; employeeId={EmployeeId}; errorCode={ErrorCode}", id, error.Code);
            return error;
        }
    }

    private static bool HasMissingRequiredField(params string?[] fields) => fields.Any(string.IsNullOrWhiteSpace);
    private static bool ValidNationalId(string value) => value.Length == 14 && value.All(char.IsDigit);
}
