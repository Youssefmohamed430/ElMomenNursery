using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElMomenNursery.Tests;

public sealed class EfUnitOfWorkTests
{
    [Fact]
    public async Task Failed_save_clears_tracked_entries_before_next_operation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NurseryDbContext>().UseSqlite(connection).Options;
        await using var db = new NurseryDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await using var unitOfWork = new EfUnitOfWork(db, NullLogger<EfUnitOfWork>.Instance);

        await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserAccountId = Guid.NewGuid(), Action = "Test.Failed", EntityType = "Test", EntityId = "1", ChangeSummary = "test"
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());

        await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            Action = "Test.Succeeded", EntityType = "Test", EntityId = "2", ChangeSummary = "test"
        });
        await unitOfWork.SaveChangesAsync();

        Assert.Equal("Test.Succeeded", (await db.AuditLogs.AsNoTracking().SingleAsync()).Action);
    }
}
