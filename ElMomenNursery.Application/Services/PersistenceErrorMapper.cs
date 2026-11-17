using ElMomenNursery.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ElMomenNursery.Application.Services;

public static class PersistenceErrorMapper
{
    public static Error FromException(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException) return PersistenceErrors.Concurrency;
        var details = exception.GetBaseException().Message;
        if (details.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase)) return PersistenceErrors.Unique;
        if (details.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase)) return PersistenceErrors.ForeignKey;
        if (details.Contains("CHECK constraint", StringComparison.OrdinalIgnoreCase)) return PersistenceErrors.Check;
        return PersistenceErrors.Unknown;
    }
}
