using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IAuthService
{
    Task<Result<bool>> NeedsInitialAdministratorSetupAsync(CancellationToken cancellationToken = default);
    Task<Result<UserSession>> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<Result> CreateInitialAdministratorAsync(string username, string fullName, string password, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
