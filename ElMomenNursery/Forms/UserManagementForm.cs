using ElMomenNursery.Application.IServices;
using ElMomenNursery.Infrastructure.Security;
using ElMomenNursery.Controls;
using ElMomenNursery.Styling;
using Serilog;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

internal sealed class UserManagementForm : UserControl
{
    private static readonly ILogger _log = Log.ForContext<UserManagementForm>();
    private readonly IServiceScopeFactory _scopes;
    private readonly IExcelExportService _excelExportService;
    private readonly UserSession _session;
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false };
    private readonly Label _status = new() { AutoSize = true, ForeColor = BrandPalette.MutedText };

    public UserManagementForm(IServiceScopeFactory scopes, UserSession session, IExcelExportService excelExportService)
    {
        _scopes = scopes; _excelExportService = excelExportService; _session = session;
        Dock = DockStyle.Fill; BackColor = BrandPalette.AppBackground; RightToLeft = RightToLeft.Yes; Padding = new Padding(24);
        _grid.Columns.Add("Username", "اسم المستخدم"); _grid.Columns.Add("FullName", "الاسم");
        _grid.Columns.Add("Roles", "الأدوار"); _grid.Columns.Add("Active", "الحالة");
        _grid.Columns.Add("Id", "Id"); _grid.Columns["Id"]!.Visible = false;
        _grid.Columns.Add("IsActive", "IsActive"); _grid.Columns["IsActive"]!.Visible = false;
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var add = Button("إضافة حساب"); var toggle = Button("تفعيل / إيقاف الحساب"); var roles = Button("صلاحيات الأدوار");
        var assignments = Button("تعيين أدوار");
        var resetPassword = Button("إعادة كلمة المرور");
        var export = Button("تصدير Excel");
        var addRole = Button("إضافة دور"); var deleteRole = Button("حذف دور");
        addRole.Visible = deleteRole.Visible = session.HasPermission("Roles.Manage");
        addRole.Click += (_, _) => RunSafeEvent(CreateRoleAsync, "Create role");
        deleteRole.Click += (_, _) => RunSafeEvent(DeleteRoleAsync, "Delete role");
        export.Visible = session.HasPermission("Users.Export");
        add.Visible = toggle.Visible = assignments.Visible = resetPassword.Visible = session.HasPermission("Users.Manage");
        roles.Visible = session.HasPermission("Permissions.Manage");
        add.Click += (_, _) => RunSafeEvent(() => AddAccountAsync(), "Create user account");
        toggle.Click += (_, _) => RunSafeEvent(() => ToggleAccountAsync(), "Change account state");
        roles.Click += (_, _) => RunSafeEvent(() => EditRolePermissionsAsync(), "Edit role permissions");
        assignments.Click += (_, _) => RunSafeEvent(() => AssignRolesAsync(), "Assign account roles");
        resetPassword.Click += (_, _) => RunSafeEvent(() => ResetPasswordAsync(), "Reset account password");
        export.Click += async (_, _) => await GridExcelExport.SaveAsync((IWin32Window?)FindForm() ?? this, _grid,
            _excelExportService, "تصدير الحسابات", "الحسابات");
        toolbar.Controls.AddRange([add, toggle, assignments, resetPassword, roles, addRole, deleteRole, export, _status]);
        Controls.Add(_grid); Controls.Add(toolbar);
        Load += (_, _) => RunSafeEvent(() => ReloadAsync(), "Load user accounts");
    }

    private async Task ReloadAsync()
    {
        try
        {
            _grid.Rows.Clear();
            var result = await WithServiceAsync(service => service.GetAccountsAsync());
            if (result.IsFailure) { _status.Text = result.Error.Description; return; }
            foreach (var user in result.Value)
            {
                var index = _grid.Rows.Add(user.Username, user.FullName, string.Join("، ", user.Roles.Select(RoleNameArabic)), user.IsActive ? "نشط" : "موقوف", user.Id, user.IsActive);
                _grid.Rows[index].Tag = user.Roles.ToArray();
            }
            _status.Text = $"عدد الحسابات: {_grid.Rows.Count}";
        }
        catch (UnauthorizedAccessException exception) { _log.Warning(exception, "Access denied while loading user accounts"); _status.Text = "ليست لديك صلاحية عرض الحسابات."; }
        catch (Exception exception) { _log.Error(exception, "Failed to load user accounts"); _status.Text = "تعذر تحميل الحسابات."; }
    }

    private async Task CreateRoleAsync()
    {
        var name = new TextBox { Left = 130, Top = 20, Width = 230 }; var description = new TextBox { Left = 130, Top = 65, Width = 230 };
        using var d = new ResponsiveForm { Text = "إضافة دور", Width = 410, Height = 190, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
        var ok = new Button { Text = "إنشاء", DialogResult = DialogResult.OK, Left = 270, Top = 110 }; d.Controls.AddRange([LabelAt("اسم الدور", 18), name, LabelAt("الوصف", 63), description, ok]);
        if (d.ShowDialog(this) != DialogResult.OK) return;
        var result = await WithServiceAsync(s => s.CreateRoleAsync(name.Text, description.Text)); if (result.IsFailure) AppDialog.ShowInformation(this, "الأدوار", result.Error.Description); else _status.Text = result.Message;
    }

    private async Task DeleteRoleAsync()
    {
        var result = await WithServiceAsync(s => s.GetRolesAsync()); if (result.IsFailure) { AppDialog.ShowInformation(this, "الأدوار", result.Error.Description); return; }
        using var d = new ResponsiveForm { Text = "حذف دور", Width = 410, Height = 150, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
        var choices = new ComboBox { Left = 25, Top = 20, Width = 340, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = result.Value.Select(x => new RoleOption(x)).ToList() }; var ok = new Button { Text = "حذف", DialogResult = DialogResult.OK, Left = 270, Top = 65 }; d.Controls.AddRange([choices, ok]);
        if (d.ShowDialog(this) != DialogResult.OK || choices.SelectedItem is not RoleOption choice) return;
        var deleted = await WithServiceAsync(s => s.DeleteRoleAsync(choice.Role.Id)); if (deleted.IsFailure) AppDialog.ShowInformation(this, "الأدوار", deleted.Error.Description); else _status.Text = deleted.Message;
    }

    private async Task AddAccountAsync()
    {
        try
        {
            var roles = await WithServiceAsync(service => service.GetRolesAsync());
            if (roles.IsFailure) { _status.Text = roles.Error.Description; return; }
            using var dialog = new ResponsiveForm { Text = "إضافة حساب", Size = new Size(430, 400), StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
            var name = Input("الاسم الكامل", 25); var username = Input("اسم المستخدم", 75); var password = Input("كلمة المرور (12 حرفًا على الأقل)", 125);
            password.UseSystemPasswordChar = true;
            var role = new ComboBox { Left = 150, Top = 175, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = roles.Value.Select(x => new RoleOption(x)).ToList() };
            var button = Button("إنشاء الحساب"); button.Left = 150; button.Top = 235; button.Click += (_, _) => dialog.DialogResult = DialogResult.OK;
            dialog.Controls.AddRange([LabelAt("الاسم الكامل", 3), name, LabelAt("اسم المستخدم", 53), username,
                LabelAt("كلمة المرور", 103), password, LabelAt("الدور", 153), role, button]);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK || role.SelectedItem is not RoleOption selected) return;
            var result = await WithServiceAsync(service => service.CreateAccountAsync(username.Text, name.Text, password.Text, selected.Role.Id));
            if (result.IsFailure) _log.Warning("Account creation was rejected; errorCode={ErrorCode}", result.Error.Code);
            if (result.IsFailure) AppDialog.ShowInformation(this, "إنشاء حساب", result.Error.Description);
            _status.Text = result.Message ?? (result.IsSuccess ? "تم إنشاء الحساب." : "تعذر إنشاء الحساب.");
            await ReloadAsync();
        }
        catch (UnauthorizedAccessException exception) { _log.Warning(exception, "Access denied while creating a user account"); _status.Text = "ليست لديك صلاحية إنشاء الحسابات."; }
        catch (Exception exception) { _log.Error(exception, "Failed to create user account"); _status.Text = "تعذر إنشاء الحساب."; }
    }

    private async Task ToggleAccountAsync()
    {
        if (_grid.SelectedRows.Count == 0) { _status.Text = "اختر حسابًا أولًا."; return; }
        var row = _grid.SelectedRows[0];
        var id = (Guid)row.Cells[4].Value!; var active = (bool)row.Cells[5].Value!;
        try
        {
            var result = await WithServiceAsync(service => service.SetAccountActiveAsync(id, !active));
            if (result.IsFailure) _log.Warning("Account status change was rejected; userId={UserId}; errorCode={ErrorCode}", id, result.Error.Code);
            if (result.IsFailure) AppDialog.ShowInformation(this, "الحسابات", result.Error.Description);
            _status.Text = result.Message ?? "تم تحديث حالة الحساب.";
            await ReloadAsync();
        }
        catch (UnauthorizedAccessException exception) { _log.Warning(exception, "Access denied while changing account status; userId={UserId}", id); _status.Text = "ليست لديك صلاحية إدارة الحسابات."; }
        catch (Exception exception) { _log.Error(exception, "Failed to change account status; userId={UserId}", id); _status.Text = "تعذر تحديث الحساب."; }
    }

    private async Task ResetPasswordAsync()
    {
        if (_grid.SelectedRows.Count == 0) { _status.Text = "اختر حسابًا أولًا."; return; }
        using var dialog = new ResponsiveForm { Text = "إعادة تعيين كلمة المرور", Size = new Size(420, 210), StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
        var password = new TextBox { Left = 160, Top = 35, Width = 220, UseSystemPasswordChar = true };
        var label = new Label { Left = 20, Top = 35, Width = 130, Text = "كلمة مرور جديدة", TextAlign = ContentAlignment.MiddleRight };
        var save = Button("حفظ كلمة المرور"); save.Left = 160; save.Top = 95; save.Click += (_, _) => dialog.DialogResult = DialogResult.OK;
        dialog.Controls.AddRange([label, password, save]);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            var id = (Guid)_grid.SelectedRows[0].Cells[4].Value!;
            var result = await WithServiceAsync(service => service.ResetPasswordAsync(id, password.Text));
            if (result.IsFailure) _log.Warning("Account password reset was rejected; userId={UserId}; errorCode={ErrorCode}", id, result.Error.Code);
            if (result.IsFailure) AppDialog.ShowInformation(this, "كلمة المرور", result.Error.Description);
            _status.Text = result.Message ?? "تم تعيين كلمة مرور جديدة.";
        }
        catch (UnauthorizedAccessException exception) { _log.Warning(exception, "Access denied while resetting account password"); _status.Text = "ليست لديك صلاحية إعادة تعيين كلمات المرور."; }
        catch (Exception exception) { _log.Error(exception, "Failed to reset account password"); _status.Text = "تعذر إعادة تعيين كلمة المرور."; }
    }

    private async Task EditRolePermissionsAsync()
    {
        try
        {
            var roles = await WithServiceAsync(service => service.GetRolesAsync());
            if (roles.IsFailure) { _status.Text = roles.Error.Description; return; }
            using var dialog = new ResponsiveForm { Text = "صلاحيات الأدوار", Size = new Size(460, 560), StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
            var select = new ComboBox { Left = 25, Top = 20, Width = 390, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = roles.Value.Select(x => new RoleOption(x)).ToList() };
            var permissions = AuthSeed.PermissionCodes.Select(code => new CheckBox { Text = PermissionNameArabic(code), AutoSize = true, Tag = code, RightToLeft = RightToLeft.Yes }).ToList();
            var list = new FlowLayoutPanel { Left = 25, Top = 62, Width = 390, Height = 390, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            list.Controls.AddRange(permissions.ToArray());
            select.SelectedIndexChanged += (_, _) =>
            {
                var current = (select.SelectedItem as RoleOption)?.Role;
                foreach (var item in permissions) item.Checked = current?.PermissionCodes.Contains((string)item.Tag!) == true;
            };
            if (select.SelectedItem is RoleOption initial)
                foreach (var item in permissions) item.Checked = initial.Role.PermissionCodes.Contains((string)item.Tag!);
            var save = Button("حفظ الصلاحيات"); save.Left = 25; save.Top = 465; save.Click += (_, _) => dialog.DialogResult = DialogResult.OK;
            dialog.Controls.AddRange([select, list, save]);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK || select.SelectedItem is not RoleOption chosen) return;
            var result = await WithServiceAsync(service => service.SetRolePermissionsAsync(chosen.Role.Id, permissions.Where(x => x.Checked).Select(x => (string)x.Tag!).ToArray()));
            if (result.IsFailure) _log.Warning("Role permission update was rejected; roleId={RoleId}; errorCode={ErrorCode}", chosen.Role.Id, result.Error.Code);
            if (result.IsFailure) AppDialog.ShowInformation(this, "صلاحيات الأدوار", result.Error.Description);
            _status.Text = result.Message ?? "تم تحديث صلاحيات الدور.";
        }
        catch (UnauthorizedAccessException exception) { _log.Warning(exception, "Access denied while managing role permissions"); _status.Text = "ليست لديك صلاحية إدارة الأدوار."; }
        catch (Exception exception) { _log.Error(exception, "Failed to load or save role permissions"); _status.Text = "تعذر تحميل أو حفظ صلاحيات الأدوار."; }
    }

    private async Task AssignRolesAsync()
    {
        if (_grid.SelectedRows.Count == 0) { _status.Text = "اختر حسابًا أولًا."; return; }
        try
        {
            var row = _grid.SelectedRows[0]; var userId = (Guid)row.Cells[4].Value!;
            var existingNames = row.Tag as string[] ?? [];
            var roles = await WithServiceAsync(service => service.GetRolesAsync());
            if (roles.IsFailure) { _status.Text = roles.Error.Description; return; }
            using var dialog = new ResponsiveForm { Text = "تعيين أدوار للحساب", Size = new Size(390, 430), StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
            var list = new CheckedListBox { Left = 25, Top = 20, Width = 320, Height = 300, CheckOnClick = true, RightToLeft = RightToLeft.Yes };
            foreach (var role in roles.Value) { var i = list.Items.Add(new RoleOption(role)); if (existingNames.Contains(role.Name, StringComparer.Ordinal)) list.SetItemChecked(i, true); }
            var save = Button("حفظ الأدوار"); save.Left = 25; save.Top = 335; save.Click += (_, _) => dialog.DialogResult = DialogResult.OK;
            dialog.Controls.AddRange([list, save]);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
            var selected = list.CheckedItems.Cast<RoleOption>().Select(x => x.Role.Id).ToArray();
            var result = await WithServiceAsync(service => service.SetUserRolesAsync(userId, selected));
            if (result.IsFailure) _log.Warning("User role update was rejected; userId={UserId}; errorCode={ErrorCode}", userId, result.Error.Code);
            if (result.IsFailure) AppDialog.ShowInformation(this, "تعيين الأدوار", result.Error.Description);
            _status.Text = result.Message ?? "تم تحديث أدوار الحساب.";
            await ReloadAsync();
        }
        catch (UnauthorizedAccessException exception) { _log.Warning(exception, "Access denied while assigning account roles"); _status.Text = "ليست لديك صلاحية تعيين الأدوار."; }
        catch (Exception exception) { _log.Error(exception, "Failed to assign account roles"); _status.Text = "تعذر تحديث أدوار الحساب."; }
    }


    private sealed record RoleOption(RoleSummary Role)
    {
        public override string ToString() => RoleNameArabic(Role.Name);
    }

    private static string RoleNameArabic(string name) => name switch
    {
        "Admin" => "مدير النظام",
        "Manager" => "مدير الحضانة",
        "Teacher" => "معلم / معلمة",
        "Accountant" => "محاسب",
        "Receptionist" => "موظف استقبال",
        _ => name
    };

    private static string PermissionNameArabic(string code)
    {
        var parts = code.Split('.', 2);
        if (parts.Length != 2) return code;
        var module = parts[0] switch
        {
            "Students" => "الأطفال", "Classes" => "الفصول", "Attendance" => "حضور الأطفال",
            "EmployeeAttendance" => "حضور الموظفين", "Employees" => "الموظفين", "Payments" => "المدفوعات",
            "Expenses" => "المصروفات", "Inventory" => "المخزن", "Subscriptions" => "الاشتراكات",
            "Users" => "المستخدمين", "Roles" => "الأدوار", "Permissions" => "الصلاحيات",
            "Settings" => "الإعدادات", "Finance" => "الماليات", "Payroll" => "الرواتب", "Lookups" => "القوائم", _ => parts[0]
        };
        var action = parts[1] switch
        {
            "View" => "عرض", "Create" => "إضافة", "Edit" => "تعديل", "Delete" => "حذف",
            "Manage" => "إدارة", "Record" => "تسجيل", "Export" => "تصدير Excel", "Restore" => "استعادة",
            "ManageIncome" => "إدارة المقبوضات", "ManageExpenses" => "إدارة مصروفات", _ => parts[1]
        };
        return $"{action} {module}";
    }

    private static Button Button(string text) => new() { Text = text, Width = 140, Height = 40, Margin = new Padding(5), FlatStyle = FlatStyle.Flat,
        BackColor = BrandPalette.Surface, ForeColor = BrandPalette.Text, UseVisualStyleBackColor = false };

    private async void RunSafeEvent(Func<Task> work, string operation)
    {
        try
        {
            await work();
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Unhandled user management event failure; operation={Operation}", operation);
            if (!IsDisposed) _status.Text = "تعذر إكمال العملية. أعد المحاولة.";
        }
    }

    private async Task<T> WithServiceAsync<T>(Func<IAccountManagementService, Task<T>> operation)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAccountManagementService>();
        return await operation(service);
    }
    private static TextBox Input(string placeholder, int top) => new() { Left = 150, Top = top, Width = 230, Height = 30, PlaceholderText = placeholder };
    private static Label LabelAt(string text, int top) => new() { Left = 25, Top = top, Width = 120, Height = 30, Text = text, TextAlign = ContentAlignment.MiddleRight };
}
