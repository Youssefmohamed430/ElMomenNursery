using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

internal sealed class PayrollPage : UserControl
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ComboBox _employee = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _paymentDate = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _filterFrom = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true };
    private readonly DateTimePicker _filterTo = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true };
    private readonly NumericUpDown _salary = MoneyInput();
    private readonly NumericUpDown _insurance = MoneyInput();
    private readonly NumericUpDown _additions = MoneyInput();
    private readonly NumericUpDown _deductions = MoneyInput();
    private readonly TextBox _notes = new() { Width = 220, MaxLength = 1000 };
    private readonly Button _savePayroll = Button("إنشاء وحساب الراتب");
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
    private IReadOnlyList<PayrollRecordDto> _records = [];
    private Guid? _editingPayrollId;

    public PayrollPage(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
        var today = DateTime.Today;
        _from.Value = new DateTime(today.Year, today.Month, 1);
        _to.Value = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        _filterFrom.Value = _from.Value; _filterTo.Value = _to.Value;
        _filterFrom.Checked = _filterTo.Checked = true;
        Dock = DockStyle.Fill; RightToLeft = RightToLeft.Yes; BackColor = Color.WhiteSmoke; Padding = new Padding(20);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, RightToLeft = RightToLeft.Yes };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var title = new Label { Text = "إدارة الرواتب", AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold), Margin = new Padding(4, 4, 4, 14) };
        var editor = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = true, RightToLeft = RightToLeft.Yes, Padding = new Padding(0, 0, 0, 12) };
        AddField(editor, "الموظف", _employee); AddField(editor, "بداية الفترة", _from); AddField(editor, "نهاية الفترة", _to);
        AddField(editor, "عرض من تاريخ", _filterFrom); AddField(editor, "إلى تاريخ", _filterTo);
        AddField(editor, "الراتب الشهري", _salary); AddField(editor, "إضافات", _additions); AddField(editor, "خصومات أخرى", _deductions);
        AddField(editor, "مصاريف التأمين", _insurance); AddField(editor, "تاريخ الصرف", _paymentDate); AddField(editor, "ملاحظات", _notes);
        _savePayroll.Click += async (_, _) => await SaveAndCalculateAsync();
        var calculate = Button("حساب المحدد"); calculate.Click += async (_, _) => await WithSelectedAsync((service, id) => service.CalculateFinalSalary(id), "تم حساب الراتب.");
        var pay = Button("صرف الراتب"); pay.Click += async (_, _) => await PaySelectedAsync();
        var edit = Button("تعديل المحدد"); edit.Click += (_, _) => BeginEdit();
        var cancelEdit = Button("إلغاء التعديل"); cancelEdit.Click += (_, _) => EndEdit();
        var refresh = Button("تحديث"); refresh.Click += async (_, _) => await LoadAsync();
        var advances = Button("سلف الموظفين"); advances.Click += async (_, _) => { using var dialog = new EmployeeAdvancesForm(_scopes); dialog.ShowDialog(this); await LoadAsync(); };
        var generate = Button("إنشاء سجلات لكل الموظفين"); generate.Click += async (_, _) => await GenerateForAllEmployeesAsync();
        var clearDateFilter = Button("إلغاء فلتر التاريخ"); clearDateFilter.Click += async (_, _) => { _filterFrom.Checked = _filterTo.Checked = false; await LoadAsync(); };
        _filterFrom.ValueChanged += async (_, _) => { if (_filterFrom.Checked) await LoadAsync(); };
        _filterTo.ValueChanged += async (_, _) => { if (_filterTo.Checked) await LoadAsync(); };
        _filterFrom.MouseUp += async (_, _) => await LoadAsync(); _filterTo.MouseUp += async (_, _) => await LoadAsync();
        editor.Controls.AddRange([_savePayroll, generate, clearDateFilter, edit, cancelEdit, calculate, pay, advances, refresh]);
        _grid.Columns.Add("Employee", "الموظف"); _grid.Columns.Add("Period", "الفترة"); _grid.Columns.Add("Salary", "الراتب الشهري");
        _grid.Columns.Add("Daily", "أجر اليوم"); _grid.Columns.Add("Absences", "أيام الغياب"); _grid.Columns.Add("Insurance", "التأمين");
        _grid.Columns.Add("Advance", "خصم السلف"); _grid.Columns.Add("Net", "صافي الراتب"); _grid.Columns.Add("Paid", "الحالة");
        _grid.SelectionChanged += (_, _) => SetEmployeeForSelected();
        _employee.SelectedIndexChanged += (_, _) =>
        {
            if (_editingPayrollId is null && _employee.SelectedItem is EmployeeDto employee) _salary.Value = Math.Min(_salary.Maximum, employee.Salary);
        };
        root.Controls.Add(title, 0, 0); root.Controls.Add(editor, 0, 1); root.Controls.Add(_grid, 0, 2); Controls.Add(root);
        Load += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using var scope = _scopes.CreateAsyncScope();
        var employees = await scope.ServiceProvider.GetRequiredService<IEmployeeService>().GetEmployees();
        if (employees.IsFailure) { AppDialog.ShowInformation(this, "الرواتب", employees.Error.Description); return; }
        var selectedId = (_employee.SelectedItem as EmployeeDto)?.Id;
        _employee.Items.Clear(); foreach (var employee in employees.Value) _employee.Items.Add(employee);
        _employee.DisplayMember = nameof(EmployeeDto.FullName);
        if (selectedId.HasValue) _employee.SelectedItem = employees.Value.FirstOrDefault(x => x.Id == selectedId.Value);
        if (_employee.SelectedIndex < 0 && _employee.Items.Count > 0) _employee.SelectedIndex = 0;
        var service = scope.ServiceProvider.GetRequiredService<IPayRollRecordService>();
        var result = await service.GetAllPayrollRecordsAsync();
        if (result.IsFailure) { AppDialog.ShowInformation(this, "الرواتب", result.Error.Description); return; }
        _records = result.Value.Where(record =>
            (!_filterFrom.Checked || record.PeriodEnd >= DateOnly.FromDateTime(_filterFrom.Value)) &&
            (!_filterTo.Checked || record.PeriodStart <= DateOnly.FromDateTime(_filterTo.Value))).ToArray(); _grid.Rows.Clear();
        foreach (var record in _records)
        {
            var index = _grid.Rows.Add(record.EmployeeName, $"{record.PeriodStart:dd/MM/yyyy} - {record.PeriodEnd:dd/MM/yyyy}", record.AgreedSalary.ToString("N2"), record.DailySalary.ToString("N2"), record.AbsenceDays, record.Insurancefunds.ToString("N2"), record.AdvanceDeduction.ToString("N2"), record.AmountPaid.ToString("N2"), record.IsPaid ? "تم الصرف" : record.IsCalculated ? "جاهز للصرف" : "غير محسوب");
            _grid.Rows[index].Tag = record.Id;
        }
    }

    private async Task GenerateForAllEmployeesAsync()
    {
        if (_from.Value.Date > _to.Value.Date) { AppDialog.ShowInformation(this, "الرواتب", "بداية الفترة يجب أن تسبق نهايتها."); return; }
        var start = DateOnly.FromDateTime(_from.Value); var end = DateOnly.FromDateTime(_to.Value);
        if (!AppDialog.Confirm(this, "إنشاء سجلات الرواتب", $"سيتم إنشاء سجل راتب لكل موظف ليس لديه سجل متداخل للفترة {start:dd/MM/yyyy} إلى {end:dd/MM/yyyy}. هل تريد المتابعة؟")) return;
        await using var scope = _scopes.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IPayRollRecordService>().CreatePayrollRecordsForAllEmployeesAsync(start, end);
        if (result.IsFailure) { AppDialog.ShowInformation(this, "الرواتب", result.Error.Description); return; }
        await LoadAsync();
        AppDialog.ShowInformation(this, "الرواتب", $"تم إنشاء {result.Value.CreatedCount} سجل، وتخطي {result.Value.SkippedCount} موظف لوجود سجل متداخل للفترة.");
    }

    private void BeginEdit()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not Guid id)
        { AppDialog.ShowInformation(this, "الرواتب", "اختر سجل راتب لتعديله."); return; }
        var record = _records.FirstOrDefault(x => x.Id == id);
        if (record is null) { AppDialog.ShowInformation(this, "الرواتب", "تعذر العثور على سجل الراتب."); return; }
        if (record.IsPaid) { AppDialog.ShowInformation(this, "الرواتب", "لا يمكن تعديل راتب تم صرفه."); return; }
        _editingPayrollId = id; _grid.Enabled = false;
        _employee.SelectedItem = _employee.Items.Cast<EmployeeDto>().FirstOrDefault(x => x.Id == record.EmployeeId);
        _from.Value = record.PeriodStart.ToDateTime(TimeOnly.MinValue); _to.Value = record.PeriodEnd.ToDateTime(TimeOnly.MinValue);
        _salary.Value = Math.Min(_salary.Maximum, record.AgreedSalary); _additions.Value = Math.Min(_additions.Maximum, record.Additions);
        _deductions.Value = Math.Min(_deductions.Maximum, record.Deductions); _insurance.Value = Math.Min(_insurance.Maximum, record.Insurancefunds);
        _paymentDate.Value = (record.PaymentDate ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue);
        _notes.Text = record.Notes ?? string.Empty; _savePayroll.Text = "حفظ التعديل وإعادة الحساب";
    }

    private void EndEdit()
    {
        _editingPayrollId = null; _grid.Enabled = true; _savePayroll.Text = "إنشاء وحساب الراتب";
        if (_employee.SelectedItem is EmployeeDto employee) _salary.Value = Math.Min(_salary.Maximum, employee.Salary);
        _additions.Value = 0; _deductions.Value = 0; _insurance.Value = 0; _notes.Clear();
        var today = DateTime.Today; _from.Value = new DateTime(today.Year, today.Month, 1);
        _to.Value = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        _paymentDate.Value = today;
    }

    private async Task SaveAndCalculateAsync()
    {
        if (_employee.SelectedItem is not EmployeeDto employee) { AppDialog.ShowInformation(this, "الرواتب", "اختر الموظف أولًا."); return; }
        if (_from.Value.Date > _to.Value.Date) { AppDialog.ShowInformation(this, "الرواتب", "بداية الفترة يجب أن تسبق نهايتها."); return; }
        await using var scope = _scopes.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IPayRollRecordService>();
        var periodStart = DateOnly.FromDateTime(_from.Value); var periodEnd = DateOnly.FromDateTime(_to.Value);
        var result = _editingPayrollId is Guid editingId
            ? await service.UpdatePayRollRecordAsync(editingId, new UpdatePayrollRecordDto
            {
                PeriodStart = periodStart, PeriodEnd = periodEnd, AgreedSalary = _salary.Value,
                Additions = _additions.Value, Deductions = _deductions.Value, Insurancefunds = _insurance.Value,
                PaymentDate = DateOnly.FromDateTime(_paymentDate.Value), Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
            })
            : await service.CreatePayRollRecord(new CreatePayrollRecordDto
            {
                EmployeeId = employee.Id, PeriodStart = periodStart, PeriodEnd = periodEnd, AgreedSalary = _salary.Value,
                Additions = _additions.Value, Deductions = _deductions.Value, Insurancefunds = _insurance.Value,
                PaymentDate = DateOnly.FromDateTime(_paymentDate.Value), Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
            });
        if (result.IsFailure) { AppDialog.ShowInformation(this, "الرواتب", result.Error.Description); return; }
        var calculated = await service.CalculateFinalSalary(result.Value.Id);
        if (calculated.IsFailure) { AppDialog.ShowInformation(this, "الرواتب", calculated.Error.Description); return; }
        EndEdit();
        await LoadAsync();
        var row = _grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(x => x.Tag is Guid id && id == result.Value.Id);
        if (row is not null) row.Selected = true;
        AppDialog.ShowInformation(this, "الرواتب", $"صافي الراتب: {calculated.Value.AmountPaid:N2} جنيه.");
    }

    private async Task WithSelectedAsync(Func<IPayRollRecordService, Guid, Task<Result<PayrollRecordDto>>> action, string success)
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not Guid payrollId)
        { AppDialog.ShowInformation(this, "الرواتب", "اختر سجل راتب."); return; }
        await using var scope = _scopes.CreateAsyncScope();
        var result = await action(scope.ServiceProvider.GetRequiredService<IPayRollRecordService>(), payrollId);
        if (result.IsFailure) { AppDialog.ShowInformation(this, "الرواتب", result.Error.Description); return; }
        await LoadAsync(); AppDialog.ShowInformation(this, "الرواتب", success);
    }

    private async Task PaySelectedAsync()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not Guid id)
        { AppDialog.ShowInformation(this, "الرواتب", "اختر سجل راتب."); return; }
        var record = _records.FirstOrDefault(x => x.Id == id);
        if (record is null) return;
        if (!record.IsCalculated)
        { AppDialog.ShowInformation(this, "الرواتب", "احسب الراتب أولًا قبل الصرف."); return; }
        var warning = record.AdvanceDeduction > 0
            ? $"تنبيه: على الموظف سلفة متبقية، وسيُخصم منها {record.AdvanceDeduction:N2} جنيه.\nصافي المبلغ الذي سيُصرف: {record.AmountPaid:N2} جنيه.\nهل تريد تسجيل الصرف؟"
            : $"صافي المبلغ الذي سيُصرف: {record.AmountPaid:N2} جنيه. هل تريد تسجيل الصرف؟";
        if (!AppDialog.Confirm(this, "تأكيد صرف الراتب", warning)) return;
        await WithSelectedAsync((service, payrollId) => service.MarkRecordAsPaid(payrollId), "تم صرف الراتب وتسجيل المصروف.");
    }

    private void SetEmployeeForSelected()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not Guid payrollId) return;
        var record = _records.FirstOrDefault(x => x.Id == payrollId);
        if (record is null) return;
        _employee.SelectedItem = _employee.Items.Cast<EmployeeDto>().FirstOrDefault(x => x.Id == record.EmployeeId);
    }
    private static void AddField(Control parent, string label, Control input) { var panel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, RightToLeft = RightToLeft.Yes, Margin = new Padding(5) }; panel.Controls.Add(new Label { Text = label, AutoSize = true }); panel.Controls.Add(input); parent.Controls.Add(panel); }
    private static Button Button(string text) => new() { Text = text, AutoSize = true, Height = 38, Margin = new Padding(5, 22, 5, 4) };
    private static NumericUpDown MoneyInput() => new() { Minimum = 0, Maximum = 100000000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 130 };
}
