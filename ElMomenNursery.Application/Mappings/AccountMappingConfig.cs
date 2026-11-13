using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Application.IServices;

namespace ElMomenNursery.Application.Mappings;

public static class AccountMappingConfig
{
    public static readonly TypeAdapterConfig Config = CreateConfig();

    private static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();

        config.NewConfig<UserAccount, AccountSummary>()
            .Map(destination => destination.Roles,
                source => source.UserRoles.Select(userRole => userRole.Role.Name)
                    .Append(source.Role.Name).Distinct().ToArray());

        config.NewConfig<Role, RoleSummary>()
            .Map(destination => destination.PermissionCodes,
                source => source.RolePermissions.Select(rolePermission => rolePermission.Permission.Code).ToArray());

        config.NewConfig<UserSessionMapping, UserSession>()
            .MapWith(source => new UserSession(source.UserId, source.Username, source.FullName,
                source.EmployeeId, source.Roles, source.Permissions, source.LoginTime));

        config.NewConfig<UserAccountMapping, UserAccount>()
            .Map(destination => destination.UsernameNormalized, source => source.Username.ToUpperInvariant())
            .Map(destination => destination.RoleId, source => source.Role.Id)
            .Map(destination => destination.IsActive, _ => true);

        config.NewConfig<UserRoleMapping, UserRole>()
            .Map(destination => destination.UserId, source => source.User.Id)
            .Map(destination => destination.RoleId, source => source.Role.Id);

        config.NewConfig<AuthenticationAuditMapping, AuthenticationAuditLog>()
            .Map(destination => destination.Timestamp,
                source => source.Timestamp ?? DateTimeOffset.UtcNow);

        return config;
    }
}

public sealed record UserSessionMapping(Guid UserId, string Username, string FullName, Guid? EmployeeId,
    IReadOnlySet<string> Roles, IReadOnlySet<string> Permissions, DateTimeOffset LoginTime);

public sealed record UserAccountMapping(string Username, string FullName, string PasswordHash, Role Role,
    DateTimeOffset CreatedAt);

public sealed record UserRoleMapping(UserAccount User, Role Role);

public sealed record AuthenticationAuditMapping(Guid? UserId, string Username, string Action, bool Success,
    string? FailureReason = null, string? Description = null, DateTimeOffset? Timestamp = null);
