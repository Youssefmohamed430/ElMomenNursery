using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ElMomenNursery.Infrastructure.Persistence;

public sealed class NurseryDbContextFactory : IDesignTimeDbContextFactory<NurseryDbContext>
{
    public NurseryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NurseryDbContext>()
            .UseSqlite("Data Source=nursery-design.db;Foreign Keys=True")
            .Options;
        return new NurseryDbContext(options);
    }
}
