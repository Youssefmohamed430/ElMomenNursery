using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElMomenNursery.Infrastructure.Security;

public static class AuthSeed
{
    public static readonly string[] PermissionCodes = [
        "Students.View", "Students.Create", "Students.Edit", "Students.Delete",
        "Classes.View", "Classes.Manage", "Attendance.View", "Attendance.Record", "Attendance.Edit",
        "Employees.View", "Employees.Manage", "Payments.View", "Payments.Create", "Payments.Edit",
        "Expenses.View", "Expenses.Manage", "Inventory.View", "Inventory.Manage",
        "Subscriptions.View", "Subscriptions.Manage", "Users.View", "Users.Manage", "Roles.Manage", "Permissions.Manage", "Settings.View", "Settings.Manage"
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
    }
}
