using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.Services;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElMomenNursery.Tests;

public sealed class ServiceResultTests
{
    [Fact]
    public async Task Child_service_returns_permission_not_found_and_validation_errors()
    {
        await using var db = await CreateDb();
        var denied = new ChildService(new TestUnitOfWork(db), NullLogger<ChildService>.Instance, new TestPermissionService("Students.View"));
        Assert.Equal(ChildErrors.PermissionDenied, (await denied.GetChildByIdAsync(Guid.NewGuid())).Error);

        var service = new ChildService(new TestUnitOfWork(db), NullLogger<ChildService>.Instance, new TestPermissionService());
        Assert.Equal(ChildErrors.NotFound, (await service.GetChildByIdAsync(Guid.NewGuid())).Error);
        Assert.Equal(ChildErrors.InvalidData, (await service.CreateChildAsync(null!)).Error);
        var invalidPhone = await service.CreateChildAsync(new CreateChildDTO
        {
            FullName = "طفل", Address = "عنوان", EnrollmentDate = DateOnly.FromDateTime(DateTime.Today),
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-4)), FatherName = "الأب", FatherPhone = "123",
            MotherName = "الأم", MotherPhone = "01100000000"
        });
        Assert.Equal(ChildErrors.InvalidData, invalidPhone.Error);

        var savedChild = new Child
        {
            FullName = "طفل", Address = "عنوان", EnrollmentDate = DateOnly.FromDateTime(DateTime.Today),
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-4)), FatherName = "الأب", FatherPhone = "01000000000",
            MotherName = "الأم", MotherPhone = "01100000000"
        };
        db.Children.Add(savedChild); await db.SaveChangesAsync();
        var editOnly = new ChildService(new TestUnitOfWork(db), NullLogger<ChildService>.Instance, new TestPermissionService("Students.Create"));
        var edit = await editOnly.UpdateChildAsync(savedChild.Id, new UpdateChildDTO
        {
            FullName = "طفل محدث", Address = "عنوان", EnrollmentDate = savedChild.EnrollmentDate,
            DateOfBirth = savedChild.DateOfBirth, FatherName = "الأب", FatherPhone = "01000000000",
            MotherName = "الأم", MotherPhone = "01100000000"
        });
        Assert.True(edit.IsSuccess);
    }

    [Fact]
    public async Task Employee_service_returns_permission_not_found_duplicate_and_salary_errors()
    {
        await using var db = await CreateDb();
        var employee = new Employee
        {
            FullName = "الموظفة", NationalIdNumber = "12345678901234", Address = "عنوان", Phone = "01000000000",
            JobDesignation = "معلمة", HireDate = DateOnly.FromDateTime(DateTime.Today)
        };
        db.Employees.Add(employee); await db.SaveChangesAsync();
        var denied = new EmployeeService(new TestUnitOfWork(db), NullLogger<EmployeeService>.Instance, new TestPermissionService("Employees.View"));
        Assert.Equal(EmployeeErrors.PermissionDenied, (await denied.GetEmployeeById(Guid.NewGuid())).Error);
        var service = new EmployeeService(new TestUnitOfWork(db), NullLogger<EmployeeService>.Instance, new TestPermissionService());
        Assert.Equal(EmployeeErrors.NotFound, (await service.GetEmployeeById(Guid.NewGuid())).Error);
        Assert.Equal(EmployeeErrors.DuplicateNationalId, (await service.CreateEmployee(new CreateEmployeeDto
        {
            FullName = "موظفة أخرى", NationalIdNumber = employee.NationalIdNumber, Address = "عنوان", Phone = "01000000000",
            JobDesignation = "معلمة", HireDate = DateOnly.FromDateTime(DateTime.Today), HasExperience = false,
            MaritalStatus = MaritalStatus.Single, Salary = 0
        })).Error);
        Assert.Equal(EmployeeErrors.InvalidSalary, (await service.CreateEmployee(new CreateEmployeeDto
        {
            FullName = "موظفة أخرى", NationalIdNumber = "22345678901234", Address = "عنوان", Phone = "01000000000",
            JobDesignation = "معلمة", HireDate = DateOnly.FromDateTime(DateTime.Today), HasExperience = false,
            MaritalStatus = MaritalStatus.Single, Salary = -1
        })).Error);
        Assert.Equal(EmployeeErrors.InvalidNationalId, (await service.CreateEmployee(new CreateEmployeeDto
        {
            FullName = "موظفة أخرى", NationalIdNumber = "1234", Address = "عنوان", Phone = "01000000000",
            JobDesignation = "معلمة", HireDate = DateOnly.FromDateTime(DateTime.Today), HasExperience = false,
            MaritalStatus = MaritalStatus.Single, Salary = 0
        })).Error);
    }

    [Theory]
    [InlineData("UNIQUE constraint failed: Employees.NationalIdNumber", "Persistence.Unique")]
    [InlineData("FOREIGN KEY constraint failed", "Persistence.ForeignKey")]
    [InlineData("CHECK constraint failed", "Persistence.Check")]
    public void Persistence_error_mapper_maps_sqlite_constraint_kinds(string message, string expectedCode)
    {
        var exception = new DbUpdateException("Database update failed", new InvalidOperationException(message));
        Assert.Equal(expectedCode, PersistenceErrorMapper.FromException(exception).Code);
    }

    [Fact]
    public void Persistence_error_mapper_maps_concurrency()
    {
        Assert.Equal(PersistenceErrors.Concurrency.Code,
            PersistenceErrorMapper.FromException(new DbUpdateConcurrencyException("Concurrency failure")).Code);
    }

    private static async Task<NurseryDbContext> CreateDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        var db = new NurseryDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
