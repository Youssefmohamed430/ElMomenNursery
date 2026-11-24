using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.Services;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElMomenNursery.Tests;

public sealed class ClassroomServiceTests
{
    [Fact]
    public async Task Every_classroom_operation_requires_ClassesManage()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new ClassroomService(new TestUnitOfWork(db), new TestPermissionService("Classes.Manage"),
            new TestSessionManager(null), NullLogger<ClassroomService>.Instance);

        Assert.Equal(ClassroomErrors.PermissionDenied, (await service.GetClassroomsAsync()).Error);
        Assert.Equal(ClassroomErrors.PermissionDenied, (await service.GetTeachersAsync()).Error);
        Assert.Equal(ClassroomErrors.PermissionDenied, (await service.SaveClassroomAsync(null, "فصل", Guid.NewGuid())).Error);
        Assert.Equal(ClassroomErrors.PermissionDenied, (await service.DeleteClassroomAsync(Guid.NewGuid())).Error);
        Assert.Equal(ClassroomErrors.PermissionDenied, (await service.GetChildrenAsync(Guid.NewGuid())).Error);
        Assert.Equal(ClassroomErrors.PermissionDenied, (await service.AssignChildrenAsync(Guid.NewGuid(), Array.Empty<Guid>())).Error);
        Assert.Equal(ClassroomErrors.PermissionDenied, (await service.GetChildClassroomAsync(Guid.NewGuid())).Error);
        Assert.Equal(ClassroomErrors.PermissionDenied, (await service.AssignChildAsync(Guid.NewGuid(), null, DateOnly.FromDateTime(DateTime.Today))).Error);
    }
}
