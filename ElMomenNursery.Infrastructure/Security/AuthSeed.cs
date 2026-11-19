using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElMomenNursery.Infrastructure.Security;

public static class AuthSeed
{
    public static readonly string[] PermissionCodes = [
        "Students.View", "Students.Create", "Students.Edit", "Students.Delete",
        "Classes.View", "Classes.Manage", "Attendance.View", "Attendance.Record", "Attendance.Edit",
        "EmployeeAttendance.View", "EmployeeAttendance.Record", "EmployeeAttendance.Edit",
        "Employees.View", "Employees.Manage", "Payments.View", "Payments.Create", "Payments.Edit",
        "Expenses.View", "Expenses.Manage", "Inventory.View", "Inventory.Manage", "Inventory.Export", "Inventory.Restore",
        "Subscriptions.View", "Subscriptions.Manage", "Users.View", "Users.Manage", "Roles.Manage", "Permissions.Manage", "Settings.View", "Settings.Manage",
        "Students.Export", "Employees.Export", "Classes.Export", "Attendance.Export", "EmployeeAttendance.Export", "Users.Export", "Subscriptions.Export",
        "Students.Restore", "Employees.Restore"
    ];

    public static async Task InitializeAsync(NurseryDbContext db, CancellationToken ct = default)
    {
        var roles = new[] { "Admin", "Manager", "Teacher", "Accountant", "Receptionist" };
        foreach (var name in roles)
            if (!await db.Roles.AnyAsync(x => x.Name == name, ct))
                db.Roles.Add(new Role { Name = name, Description = $"دور نظامي: {name}", IsSystemRole = true });

        var specs = PermissionCodes.Select(code => new Permission { Code = code, Name = code, Module = code.Split('.')[0] }).ToArray();
        foreach (var p in specs)
            if (!await db.Permissions.AnyAsync(x => x.Code == p.Code, ct)) db.Permissions.Add(p);
        await db.SaveChangesAsync(ct);

        var mappings = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Admin"] = PermissionCodes,
            ["Manager"] = ["Students.View", "Students.Create", "Students.Edit", "Classes.View", "Classes.Manage", "Attendance.View", "Attendance.Record", "Attendance.Edit", "Employees.View", "Employees.Manage", "Payments.View", "Expenses.View", "Inventory.View", "Inventory.Manage", "Subscriptions.View", "Subscriptions.Manage"],
            ["Teacher"] = ["Students.View", "Classes.View", "Attendance.View", "Attendance.Record"],
            ["Accountant"] = ["Payments.View", "Payments.Create", "Payments.Edit", "Expenses.View", "Expenses.Manage", "Subscriptions.View"],
            ["Receptionist"] = ["Students.View", "Students.Create", "Classes.View", "Subscriptions.View"]
        };
        var seeded = await db.AppSettings.AnyAsync(x => x.Key == "Authentication.RolePermissionsSeeded.V1", ct);
        if (!seeded)
        {
            foreach (var (roleName, codes) in mappings)
            {
                var role = await db.Roles.Include(x => x.RolePermissions).SingleAsync(x => x.Name == roleName, ct);
                if (role.RolePermissions.Count != 0) continue;
                var permissionIds = await db.Permissions.Where(p => codes.Contains(p.Code)).Select(p => p.Id).ToListAsync(ct);
                foreach (var permissionId in permissionIds) db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
            }
            db.AppSettings.Add(new AppSetting { Key = "Authentication.RolePermissionsSeeded.V1", Value = "true",
                Description = "Initial role permission mapping seed completed." });
            await db.SaveChangesAsync(ct);
        }

        // Add employee attendance access to Manager without replacing administrator-edited mappings.
        var v2Seeded = await db.AppSettings.AnyAsync(x => x.Key == "Authentication.RolePermissionsSeeded.V2", ct);
        if (!v2Seeded)
        {
            var manager = await db.Roles.Include(x => x.RolePermissions).SingleAsync(x => x.Name == "Manager", ct);
            var managerCodes = await db.RolePermissions.Where(x => x.RoleId == manager.Id)
                .Select(x => x.Permission.Code).ToListAsync(ct);
            var missingCodes = new[] { "EmployeeAttendance.View", "EmployeeAttendance.Record", "EmployeeAttendance.Edit" }
                .Except(managerCodes, StringComparer.Ordinal).ToArray();
            var permissionIds = await db.Permissions.Where(x => missingCodes.Contains(x.Code)).Select(x => x.Id).ToListAsync(ct);
            foreach (var permissionId in permissionIds)
                db.RolePermissions.Add(new RolePermission { RoleId = manager.Id, PermissionId = permissionId });
            db.AppSettings.Add(new AppSetting
            {
                Key = "Authentication.RolePermissionsSeeded.V2",
                Value = "true",
                Description = "Employee attendance permission seed completed without replacing existing mappings."
            });
            await db.SaveChangesAsync(ct);
        }

        // Grant current list-export permissions to the built-in Admin and Manager roles once.
        var v3Seeded = await db.AppSettings.AnyAsync(x => x.Key == "Authentication.RolePermissionsSeeded.V3", ct);
        if (!v3Seeded)
        {
            var exportsByRole = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["Admin"] = ["Students.Export", "Employees.Export", "Classes.Export", "Attendance.Export", "EmployeeAttendance.Export", "Users.Export"],
                ["Manager"] = ["Students.Export", "Employees.Export", "Classes.Export", "Attendance.Export", "EmployeeAttendance.Export"]
            };
            foreach (var (roleName, codes) in exportsByRole)
            {
                var role = await db.Roles.Include(x => x.RolePermissions).SingleAsync(x => x.Name == roleName, ct);
                var assignedCodes = (await db.RolePermissions.Where(x => x.RoleId == role.Id)
                    .Select(x => x.Permission.Code).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);
                var permissionIds = await db.Permissions.Where(x => codes.Contains(x.Code) && !assignedCodes.Contains(x.Code))
                    .Select(x => x.Id).ToListAsync(ct);
                foreach (var permissionId in permissionIds)
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
            }
            db.AppSettings.Add(new AppSetting
            {
                Key = "Authentication.RolePermissionsSeeded.V3",
                Value = "true",
                Description = "Initial list export permission seed completed without replacing administrator-edited mappings."
            });
            await db.SaveChangesAsync(ct);
        }

        var v4Seeded = await db.AppSettings.AnyAsync(x => x.Key == "Authentication.RolePermissionsSeeded.V4", ct);
        if (!v4Seeded)
        {
            var permission = await db.Permissions.SingleAsync(x => x.Code == "Subscriptions.Export", ct);
            foreach (var roleName in new[] { "Admin", "Manager" })
            {
                var role = await db.Roles.SingleAsync(x => x.Name == roleName, ct);
                if (!await db.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permission.Id, ct))
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            }
            db.AppSettings.Add(new AppSetting { Key = "Authentication.RolePermissionsSeeded.V4", Value = "true",
                Description = "Subscription export permission added to Admin and Manager without replacing existing mappings." });
            await db.SaveChangesAsync(ct);
        }

        var v5Seeded = await db.AppSettings.AnyAsync(x => x.Key == "Authentication.RolePermissionsSeeded.V5", ct);
        if (!v5Seeded)
        {
            var admin = await db.Roles.Include(x => x.RolePermissions).SingleAsync(x => x.Name == "Admin", ct);
            var assignedCodes = (await db.RolePermissions.Where(x => x.RoleId == admin.Id)
                .Select(x => x.Permission.Code).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);
            var permissionIds = await db.Permissions.Where(x => new[] { "Students.Restore", "Employees.Restore" }.Contains(x.Code) && !assignedCodes.Contains(x.Code))
                .Select(x => x.Id).ToListAsync(ct);
            foreach (var permissionId in permissionIds)
                db.RolePermissions.Add(new RolePermission { RoleId = admin.Id, PermissionId = permissionId });
            db.AppSettings.Add(new AppSetting { Key = "Authentication.RolePermissionsSeeded.V5", Value = "true",
                Description = "Soft-delete restore permissions added to Admin without replacing administrator-edited mappings." });
            await db.SaveChangesAsync(ct);
        }

        var v6Seeded = await db.AppSettings.AnyAsync(x => x.Key == "Authentication.RolePermissionsSeeded.V6", ct);
        if (!v6Seeded)
        {
            foreach (var (roleName, code) in new[] { ("Admin", "Inventory.Export"), ("Manager", "Inventory.Export"), ("Admin", "Inventory.Restore") })
            {
                var role = await db.Roles.SingleAsync(x => x.Name == roleName, ct);
                var permission = await db.Permissions.SingleAsync(x => x.Code == code, ct);
                if (!await db.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permission.Id, ct))
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            }
            db.AppSettings.Add(new AppSetting { Key = "Authentication.RolePermissionsSeeded.V6", Value = "true",
                Description = "Inventory export and restore permissions added without replacing administrator-edited mappings." });
            await db.SaveChangesAsync(ct);
        }
    }
}
