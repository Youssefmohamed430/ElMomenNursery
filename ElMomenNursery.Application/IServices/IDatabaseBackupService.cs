using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IDatabaseBackupService
{
    Task<Result> CreateBackupAsync(string destinationPath, CancellationToken cancellationToken = default);
}
