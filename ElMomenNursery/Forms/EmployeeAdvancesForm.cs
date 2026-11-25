using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

internal sealed class EmployeeAdvancesForm : ResponsiveForm
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ComboBox _employee = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
    private readonly NumericUpDown _amount = new() { Minimum = 0.01m, Maximum = 100000000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 150 };
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today };
    private readonly TextBox _notes = new() { Width = 250, MaxLength = 1000 };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect };

    public EmployeeAdvancesForm(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
        Text = "سلف الموظفين"; StartPosition = FormStartPosition.CenterParent; MinimumSize = new Size(850, 520); Size = new Size(1040, 650);
        RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; Font = new Font("Segoe UI", 10); BackColor = Color.WhiteSmoke;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(14), RightToLeft = RightToLeft.Yes };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var editor = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = true, RightToLeft = RightToLeft.Yes };
        AddField(editor, "الموظف", _employee); AddField(editor, "مبلغ السلفة", _amount); AddField(editor, "تاريخ السلفة", _date); AddField(editor, "ملاحظات", _notes);
        var add = new Button { Text = "تسجيل السلفة", AutoSize = true, Height = 36, Margin = new Padding(6, 22, 6, 4) };
        add.Click += (_, _) => _ = UiGuard.RunAsync(this, "EmployeeAdvances.Create", CreateAdvanceAsync); editor.Controls.Add(add);
        foreach (var (name, caption) in new[] { ("Employee", "الموظف"), ("Date", "التاريخ"), ("Amount", "قيمة السلفة"), ("Remaining", "الرصيد المتبقي"), ("Status", "الحالة"), ("Notes", "ملاحظات") }) _grid.Columns.Add(name, caption);
        root.Controls.Add(editor, 0, 0); root.Controls.Add(_grid, 0, 1); Controls.Add(root);
        Load += (_, _) => _ = UiGuard.RunAsync(this, "EmployeeAdvances.Load", LoadDataAsync);
    }

    private async Task LoadDataAsync()
    {
        await UiGuard.RunAsync(this, "EmployeeAdvances.Load", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var employees = await scope.ServiceProvider.GetRequiredService<IEmployeeService>().GetEmployees();
            if (employees.IsFailure) { AppDialog.ShowInformation(this, "السلف", employees.Error.Description); return; }
            var selectedId = (_employee.SelectedItem as EmployeeDto)?.Id;
            _employee.Items.Clear(); foreach (var employee in employees.Value) _employee.Items.Add(employee);
            _employee.DisplayMember = nameof(EmployeeDto.FullName);
            if (selectedId.HasValue) _employee.SelectedItem = employees.Value.FirstOrDefault(x => x.Id == selectedId.Value);
            if (_employee.SelectedIndex < 0 && _employee.Items.Count > 0) _employee.SelectedIndex = 0;
            var advances = await scope.ServiceProvider.GetRequiredService<IPayRollRecordService>().GetEmployeeAdvancesAsync();
            if (advances.IsFailure) { AppDialog.ShowInformation(this, "السلف", advances.Error.Description); return; }
            _grid.Rows.Clear();
            foreach (var advance in advances.Value)
                _grid.Rows.Add(advance.EmployeeName, advance.AdvanceDate.ToString("dd/MM/yyyy"), advance.OriginalAmount.ToString("N2"),
                    advance.RemainingAmount.ToString("N2"), advance.RemainingAmount == 0 ? "تم السداد" : "مستحقة", advance.Notes ?? "");
        }, "تعذر تحميل بيانات السلف.");
    }

    private async Task CreateAdvanceAsync()
    {
        if (_employee.SelectedItem is not EmployeeDto employee) { AppDialog.ShowInformation(this, "السلف", "اختر الموظف أولًا."); return; }
        await UiGuard.RunAsync(this, "EmployeeAdvances.Create", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IPayRollRecordService>().CreateEmployeeAdvanceAsync(new CreateEmployeeAdvanceDto
            {
                EmployeeId = employee.Id, Amount = _amount.Value, AdvanceDate = DateOnly.FromDateTime(_date.Value),
                Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
            });
            if (result.IsFailure) { AppDialog.ShowInformation(this, "السلف", result.Error.Description); return; }
            _amount.Value = 0.01m; _notes.Clear(); await LoadDataAsync();
            AppDialog.ShowInformation(this, "السلف", "تم تسجيل السلفة وإضافتها للمصروفات، وسيُخصم رصيدها من الراتب عند الصرف.");
        }, "تعذر تسجيل السلفة.");
    }

    private static void AddField(Control parent, string label, Control input)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, RightToLeft = RightToLeft.Yes, Margin = new Padding(5) };
        panel.Controls.Add(new Label { Text = label, AutoSize = true }); panel.Controls.Add(input); parent.Controls.Add(panel);
    }
}
