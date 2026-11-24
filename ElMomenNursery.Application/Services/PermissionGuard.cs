namespace ElMomenNursery.Application.Services;

public static class PermissionGuard
{
    public static Error? Check(IPermissionService permissions, string permissionCode)
        => Check(permissions, permissionCode, CommonErrors.PermissionDenied);

    public static Error? Check(IPermissionService permissions, string permissionCode, Error deniedError)
        => permissions.HasPermission(permissionCode) ? null : deniedError;
}
