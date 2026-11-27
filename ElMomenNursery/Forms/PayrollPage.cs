using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using System.Globalization;
using System.Text.Json;
using ElMomenNursery.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

internal sealed class PayrollPage : UserControl
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IExcelExportService _excel;
    private readonly ComboBox _employee = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _paymentDate = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _filterFrom = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true };
    private readonly DateTimePicker _filterTo = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true };
    private readonly Label _editingEmployeeName = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Visible = false };
    private readonly NumericUpDown _salary = MoneyInput();
    private readonly NumericUpDown _insurance = MoneyInput();
    private readonly NumericUpDown _additions = MoneyInput();
    private readonly NumericUpDown _deductions = MoneyInput();
    private readonly TextBox _notes = new() { Width = 220, MaxLength = 1000 };
    private readonly Button _savePayroll = Button("إنشاء وحساب الراتب");
    private readonly Label _filteredTotal = new() { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), Margin = new Padding(8, 25, 8, 4) };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
    private readonly Panel _canvas = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly Dictionary<string, Control> _layoutControls = new(StringComparer.Ordinal);
    private Dictionary<string, int[]> _savedLayout = new(StringComparer.Ordinal);
    private bool _isCustomizing;
    private bool _hasCustomLayout;
    private int _defaultGridTop = 300;
    private IReadOnlyList<PayrollRecordDto> _records = [];
    private Guid? _editingPayrollId;

    public PayrollPage(IServiceScopeFactory scopes, IExcelExportService excel)
    {
        _scopes = scopes; _excel = excel;
        var today = DateTime.Today;
        _from.Value = new DateTime(today.Year, today.Month, 1);
        _to.Value = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        _filterFrom.Value = _from.Value; _filterTo.Value = _to.Value;
        _filterFrom.Checked = _filterTo.Checked = true;
        Dock = DockStyle.Fill; RightToLeft = RightToLeft.Yes; BackColor = Color.WhiteSmoke; Padding = new Padding(12);

        var title = new Label { Name = "heading.title", Text = "إدارة الرواتب", AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
        var subtitle = new Label { Name = "heading.subtitle", Text = "إنشاء ومراجعة وصرف رواتب الموظفين", AutoSize = true, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft };
        RegisterLayoutControl("heading.title", title);
        RegisterLayoutControl("heading.subtitle", subtitle);

        var employeeField = AddPayrollField("field.employee", "الموظف", _employee, 260);
        employeeField.Controls.Add(_editingEmployeeName);
        AddPayrollField("field.period-start", "بداية الفترة", _from, 170);
        AddPayrollField("field.period-end", "نهاية الفترة", _to, 170);
        AddPayrollField("field.filter-from", "عرض من تاريخ", _filterFrom, 170);
        AddPayrollField("field.filter-to", "إلى تاريخ", _filterTo, 170);
        AddPayrollField("field.salary", "الراتب الشهري", _salary, 140);
        AddPayrollField("field.additions", "إضافات", _additions, 140);
        AddPayrollField("field.deductions", "خصومات أخرى", _deductions, 140);
        AddPayrollField("field.insurance", "مصاريف التأمين", _insurance, 140);
        AddPayrollField("field.payment-date", "تاريخ الصرف", _paymentDate, 170);
        AddPayrollField("field.notes", "ملاحظات", _notes, 240);

        _savePayroll.Name = "action.save";
        var calculate = Button("حساب المحدد"); calculate.Name = "action.calculate";
        var pay = Button("صرف الراتب"); pay.Name = "action.pay";
        var edit = Button("تعديل المحدد"); edit.Name = "action.edit";
        var cancelEdit = Button("إلغاء التعديل"); cancelEdit.Name = "action.cancel-edit";
        var refresh = Button("تحديث"); refresh.Name = "action.refresh";
        var clearDateFilter = Button("إلغاء فلتر التاريخ"); clearDateFilter.Name = "action.clear-filter";
        var export = Button("تصدير Excel"); export.Name = "action.export";
        var customize = Button("تخصيص الشاشة"); customize.Name = "layout.customize"; customize.Width = 150; customize.AutoSize = false;
        var resetLayout = Button("استعادة الافتراضي"); resetLayout.Name = "layout.reset"; resetLayout.Width = 160; resetLayout.AutoSize = false; resetLayout.Visible = false;
        var layoutStatus = new Label { Name = "layout.status", AutoSize = true, ForeColor = Color.DarkSlateBlue, Text = "" };

        RegisterLayoutControl("action.save", _savePayroll);
        RegisterLayoutControl("action.calculate", calculate);
        RegisterLayoutControl("action.pay", pay);
        RegisterLayoutControl("action.edit", edit);
        RegisterLayoutControl("action.cancel-edit", cancelEdit);
        RegisterLayoutControl("action.refresh", refresh);
        RegisterLayoutControl("action.clear-filter", clearDateFilter);
        RegisterLayoutControl("action.export", export);
        RegisterLayoutControl("summary.total", _filteredTotal);
        RegisterLayoutControl("payroll.grid", _grid);

        _savePayroll.Click += (_, _) => { if (!_isCustomizing) _ = UiGuard.RunAsync(this, "Payroll.SaveAndCalculate", SaveAndCalculateAsync); };
        calculate.Click += (_, _) => { if (!_isCustomizing) _ = UiGuard.RunAsync(this, "Payroll.CalculateSelected", () => WithSelectedAsync((service, id) => service.CalculateFinalSalary(id), "تم حساب الراتب.")); };
        pay.Click += (_, _) => { if (!_isCustomizing) _ = UiGuard.RunAsync(this, "Payroll.PaySelected", PaySelectedAsync); };
        edit.Click += (_, _) => { if (!_isCustomizing) BeginEdit(); };
        cancelEdit.Click += (_, _) => { if (!_isCustomizing) EndEdit(); };
        refresh.Click += (_, _) => { if (!_isCustomizing) _ = UiGuard.RunAsync(this, "Payroll.Load", LoadAsync); };
        clearDateFilter.Click += (_, _) =>
        {
            if (_isCustomizing) return;
            _filterFrom.Checked = _filterTo.Checked = false;
            _ = UiGuard.RunAsync(this, "Payroll.Load", LoadAsync);
        };
        export.Click += async (_, _) => { if (!_isCustomizing) await ExportAsync(); };
        customize.Click += (_, _) => ToggleCustomization(customize, resetLayout, layoutStatus);
        resetLayout.Click += (_, _) => ResetLayout(customize, resetLayout, layoutStatus);
        _filterFrom.ValueChanged += (_, _) => { if (!_isCustomizing && _filterFrom.Checked) _ = UiGuard.RunAsync(this, "Payroll.Load", LoadAsync); };
        _filterTo.ValueChanged += (_, _) => { if (!_isCustomizing && _filterTo.Checked) _ = UiGuard.RunAsync(this, "Payroll.Load", LoadAsync); };
        _filterFrom.MouseUp += (_, _) => { if (!_isCustomizing) _ = UiGuard.RunAsync(this, "Payroll.Load", LoadAsync); };
        _filterTo.MouseUp += (_, _) => { if (!_isCustomizing) _ = UiGuard.RunAsync(this, "Payroll.Load", LoadAsync); };

        _grid.Columns.Add("Employee", "الموظف"); _grid.Columns.Add("Period", "الفترة"); _grid.Columns.Add("Salary", "الراتب الشهري");
        _grid.Columns.Add("Daily", "أجر اليوم"); _grid.Columns.Add("Absences", "أيام الغياب"); _grid.Columns.Add("LateMinutes", "دقائق التأخير"); _grid.Columns.Add("LateDeduction", "خصم التأخير"); _grid.Columns.Add("Prorated", "الراتب حسب مدة العمل"); _grid.Columns.Add("Insurance", "التأمين");
        _grid.Columns.Add("Deductions", "خصومات أخرى"); _grid.Columns.Add("Additions", "إضافات"); _grid.Columns.Add("Advance", "خصم السلف"); _grid.Columns.Add("Net", "صافي الراتب"); _grid.Columns.Add("Paid", "الحالة");
        _grid.SelectionChanged += (_, _) => SetEmployeeForSelected();
        _employee.SelectedIndexChanged += (_, _) =>
        {
            if (!_isCustomizing && _editingPayrollId is null && _employee.SelectedItem is EmployeeDto employee) _salary.Value = Math.Min(_salary.Maximum, employee.Salary);
        };

        _canvas.Controls.AddRange([customize, resetLayout, layoutStatus]);
        Controls.Add(_canvas);
        foreach (var control in _layoutControls.Values) _canvas.Controls.Add(control);
        _savedLayout = LoadSavedLayout();
        _hasCustomLayout = _layoutControls.Keys.All(name => _savedLayout.TryGetValue(name, out var bounds)
            && bounds is { Length: 4 } && bounds[2] >= 50 && bounds[3] >= 28);
        AttachDragHandlers();
        customize.BringToFront(); resetLayout.BringToFront();
        Resize += (_, _) => ApplyPayrollLayout(customize, resetLayout, layoutStatus);
        _canvas.Resize += (_, _) => ApplyPayrollLayout(customize, resetLayout, layoutStatus);
        ApplyPayrollLayout(customize, resetLayout, layoutStatus);
        Load += (_, _) => _ = UiGuard.RunAsync(this, "Payroll.InitialLoad", LoadAsync);
    }

    private Panel AddPayrollField(string name, string caption, Control input, int width)
    {
        var field = new Panel { Name = name, Width = width, Height = 66, Margin = new Padding(4), Padding = new Padding(2), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
        var label = new Label { Text = caption, Dock = DockStyle.Top, Height = 22, TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = true };
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0);
        field.Controls.Add(input);
        field.Controls.Add(label);
        _layoutControls.Add(name, field);
        return field;
    }

    private void RegisterLayoutControl(string name, Control control)
    {
        control.Name = name;
        control.Dock = DockStyle.None;
        control.AutoSize = false;
        _layoutControls.Add(name, control);
    }

    private void ApplyPayrollLayout(Button customize, Button resetLayout, Label status)
    {
        if (_isCustomizing || _canvas.ClientSize.Width <= 0 || _canvas.ClientSize.Height <= 0) return;
        _canvas.SuspendLayout();
        if (_hasCustomLayout)
            ApplySavedPayrollLayout();
        else
            ArrangeDefaultPayrollLayout(customize, resetLayout, status);

        if (_hasCustomLayout)
        {
            customize.SetBounds(12, 4, 150, 38);
            resetLayout.SetBounds(170, 4, 160, 38);
        }
        customize.BringToFront();
        resetLayout.BringToFront();
        _canvas.AutoScrollMinSize = new Size(_canvas.ClientSize.Width, Math.Max(_canvas.ClientSize.Height, _grid.Bottom + 16));
        _canvas.ResumeLayout(performLayout: true);
    }

    private void ArrangeDefaultPayrollLayout(Button customize, Button resetLayout, Label status)
    {
        var width = _canvas.ClientSize.Width;
        const int margin = 18;
        _layoutControls["heading.title"].SetBounds(Math.Max(margin, width - _layoutControls["heading.title"].Width - margin), 4,
            _layoutControls["heading.title"].Width, 34);
        _layoutControls["heading.subtitle"].SetBounds(margin, 8, _layoutControls["heading.subtitle"].Width, 26);

        var fieldNames = new[] { "field.employee", "field.period-start", "field.period-end", "field.filter-from", "field.filter-to", "field.salary", "field.additions", "field.deductions", "field.insurance", "field.payment-date", "field.notes" };
        var xRight = width - margin;
        var y = 52;
        var rowHeight = 66;
        foreach (var name in fieldNames)
        {
            var field = _layoutControls[name];
            var x = xRight - field.Width;
            if (x < margin && xRight < width - margin)
            {
                y += rowHeight + 8;
                xRight = width - margin;
                x = xRight - field.Width;
            }
            field.SetBounds(Math.Max(margin, x), y, field.Width, rowHeight);
            xRight = x - 8;
        }

        var fieldsBottom = _layoutControls[fieldNames[^1]].Bottom;
        var firstActionY = fieldsBottom + 14;
        PlaceButtonsFromRight(["action.save", "action.clear-filter", "action.export"], firstActionY, width - margin);
        _layoutControls["summary.total"].SetBounds(margin, firstActionY, Math.Min(410, Math.Max(250, width / 3)), 38);

        var secondActionY = firstActionY + 48;
        PlaceButtonsFromRight(["action.edit", "action.cancel-edit", "action.calculate", "action.pay", "action.refresh"], secondActionY, width - margin);
        customize.SetBounds(margin, 4, 150, 38);
        resetLayout.SetBounds(margin + 158, 4, 160, 38);
        status.SetBounds(margin, secondActionY + 40, 420, 26);
        status.Visible = false;

        _defaultGridTop = secondActionY + 50;
        var grid = _layoutControls["payroll.grid"];
        grid.SetBounds(margin, _defaultGridTop, Math.Max(300, width - (margin * 2)), Math.Max(220, _canvas.ClientSize.Height - _defaultGridTop - margin));
    }

    private void PlaceButtonsFromRight(IReadOnlyList<string> names, int top, int right)
    {
        foreach (var name in names)
        {
            var button = _layoutControls[name];
            var preferred = button.GetPreferredSize(Size.Empty);
            var buttonWidth = Math.Max(name == "action.save" ? 175 : 105, preferred.Width);
            button.SetBounds(right - buttonWidth, top, buttonWidth, 38);
            right -= buttonWidth + 8;
        }
    }

    private void ApplySavedPayrollLayout()
    {
        foreach (var (name, control) in _layoutControls)
        {
            if (!_savedLayout.TryGetValue(name, out var bounds) || bounds.Length != 4) continue;
            var width = Math.Clamp(bounds[2], 50, Math.Max(50, _canvas.ClientSize.Width));
            var height = Math.Clamp(bounds[3], 28, Math.Max(28, _canvas.ClientSize.Height));
            var x = Math.Clamp(bounds[0], 0, Math.Max(0, _canvas.ClientSize.Width - width));
            var y = Math.Clamp(bounds[1], 0, Math.Max(0, _canvas.ClientSize.Height - height));
            control.SetBounds(x, y, width, height);
        }
    }

    private void ToggleCustomization(Button customize, Button resetLayout, Label status)
    {
        if (!_isCustomizing)
        {
            _isCustomizing = true;
            customize.Text = "حفظ التخطيط";
            resetLayout.Visible = true;
            status.Text = "وضع التخصيص: اسحب الحقول والأزرار والجدول ثم احفظ.";
            status.Visible = true;
            SetLayoutCursor(Cursors.SizeAll);
            customize.BringToFront(); resetLayout.BringToFront(); status.BringToFront();
            return;
        }

        if (!SavePayrollLayout()) return;
        _savedLayout = ReadPayrollLayout();
        _hasCustomLayout = true;
        _isCustomizing = false;
        customize.Text = "تخصيص الشاشة";
        resetLayout.Visible = false;
        status.Visible = false;
        SetLayoutCursor(Cursors.Default);
        ApplyPayrollLayout(customize, resetLayout, status);
    }

    private void ResetLayout(Button customize, Button resetLayout, Label status)
    {
        try
        {
            var path = GetPayrollLayoutPath();
            if (File.Exists(path)) File.Delete(path);
            _savedLayout.Clear();
            _hasCustomLayout = false;
            _isCustomizing = false;
            customize.Text = "تخصيص الشاشة";
            resetLayout.Visible = false;
            status.Visible = false;
            SetLayoutCursor(Cursors.Default);
            ApplyPayrollLayout(customize, resetLayout, status);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"تعذر استعادة التخطيط الافتراضي: {exception.Message}", "تخصيص شاشة الرواتب",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
        }
    }

    private bool SavePayrollLayout()
    {
        try
        {
            var path = GetPayrollLayoutPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(ReadPayrollLayout()));
            return true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"تعذر حفظ تخطيط الشاشة: {exception.Message}", "تخصيص شاشة الرواتب",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return false;
        }
    }

    private Dictionary<string, int[]> ReadPayrollLayout() => _layoutControls.ToDictionary(
        pair => pair.Key,
        pair => new[] { pair.Value.Left, pair.Value.Top, pair.Value.Width, pair.Value.Height },
        StringComparer.Ordinal);

    private Dictionary<string, int[]> LoadSavedLayout()
    {
        try
        {
            var path = GetPayrollLayoutPath();
            if (!File.Exists(path)) return new Dictionary<string, int[]>(StringComparer.Ordinal);
            return JsonSerializer.Deserialize<Dictionary<string, int[]>>(File.ReadAllText(path))
                ?? new Dictionary<string, int[]>(StringComparer.Ordinal);
        }
        catch
        {
            return new Dictionary<string, int[]>(StringComparer.Ordinal);
        }
    }

    private static string GetPayrollLayoutPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlMoumenNursery", "payroll-screen-layout.json");

    private void AttachDragHandlers()
    {
        foreach (var root in _layoutControls.Values)
            AttachDragHandler(root, root);
    }

    private void AttachDragHandler(Control root, Control target)
    {
        var dragging = false;
        var offset = Point.Empty;
        target.MouseDown += (_, e) =>
        {
            if (!_isCustomizing || e.Button != MouseButtons.Left) return;
            var point = _canvas.PointToClient(target.PointToScreen(e.Location));
            offset = new Point(point.X - root.Left, point.Y - root.Top);
            dragging = true;
        };
        target.MouseMove += (_, e) =>
        {
            if (!dragging || !_isCustomizing) return;
            var point = _canvas.PointToClient(target.PointToScreen(e.Location));
            var x = Math.Clamp(point.X - offset.X, 0, Math.Max(0, _canvas.ClientSize.Width - root.Width));
            var y = Math.Clamp(point.Y - offset.Y, 0, Math.Max(0, _canvas.ClientSize.Height - root.Height));
            root.Location = new Point(x, y);
        };
        target.MouseUp += (_, _) => dragging = false;
        foreach (Control child in target.Controls)
            AttachDragHandler(root, child);
    }

    private void SetLayoutCursor(Cursor cursor)
    {
        foreach (var control in _layoutControls.Values)
            SetCursorRecursive(control, cursor);
    }

    private static void SetCursorRecursive(Control control, Cursor cursor)
    {
        control.Cursor = cursor;
        foreach (Control child in control.Controls)
            SetCursorRecursive(child, cursor);
    }

    private async Task LoadAsync()
    {
        await UiGuard.RunAsync(this, "Payroll.Load", async () =>
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
                var index = _grid.Rows.Add(record.EmployeeName, $"{record.PeriodStart:dd/MM/yyyy} - {record.PeriodEnd:dd/MM/yyyy}", record.AgreedSalary.ToString("N2"), record.DailySalary.ToString("N2"), record.AbsenceDays, record.LateMinutes, record.LateDeduction.ToString("N2"), record.ProratedSalary.ToString("N2"), record.InsuranceFunds.ToString("N2"), record.Deductions.ToString("N2"), record.Additions.ToString("N2"), record.AdvanceDeduction.ToString("N2"), record.AmountPaid.ToString("N2"), record.IsPaid ? "تم الصرف" : record.IsCalculated ? "جاهز للصرف" : "غير محسوب");
                _grid.Rows[index].Tag = record.Id;
            }
            _filteredTotal.Text = $"إجمالي صافي الرواتب المعروضة: {_records.Sum(x => x.AmountPaid):N2} ج.م";
        }, "تعذر تحميل بيانات الرواتب.");
    }

    private async Task ExportAsync()
    {
        var start = _filterFrom.Checked ? DateOnly.FromDateTime(_filterFrom.Value) : (DateOnly?)null;
        var end = _filterTo.Checked ? DateOnly.FromDateTime(_filterTo.Value) : (DateOnly?)null;
        var description = start is null && end is null ? "كل الفترات" : $"من {start?.ToString("dd/MM/yyyy") ?? "البداية"} إلى {end?.ToString("dd/MM/yyyy") ?? "النهاية"}";
        var headers = new[] { "الموظف", "الفترة", "الراتب الشهري", "الغياب بالأيام", "دقائق التأخير", "خصم التأخير", "الراتب حسب مدة العمل", "خصومات أخرى", "إضافات", "التأمين", "خصم السلف", "صافي الراتب", "الحالة" };
        var rows = _records.Select(x => (IReadOnlyList<string?>)[x.EmployeeName, $"{x.PeriodStart:dd/MM/yyyy} - {x.PeriodEnd:dd/MM/yyyy}", x.AgreedSalary.ToString("N2", CultureInfo.CurrentCulture), x.AbsenceDays.ToString(CultureInfo.CurrentCulture), x.LateMinutes.ToString(CultureInfo.CurrentCulture), x.LateDeduction.ToString("N2", CultureInfo.CurrentCulture), x.ProratedSalary.ToString("N2", CultureInfo.CurrentCulture), x.Deductions.ToString("N2", CultureInfo.CurrentCulture), x.Additions.ToString("N2", CultureInfo.CurrentCulture), x.InsuranceFunds.ToString("N2", CultureInfo.CurrentCulture), x.AdvanceDeduction.ToString("N2", CultureInfo.CurrentCulture), x.AmountPaid.ToString("N2", CultureInfo.CurrentCulture), x.IsPaid ? "تم الصرف" : x.IsCalculated ? "جاهز للصرف" : "غير محسوب"]).ToList();
        rows.Add(["الإجمالي", "", "", "", "", _records.Sum(x => x.LateDeduction).ToString("N2", CultureInfo.CurrentCulture), _records.Sum(x => x.ProratedSalary).ToString("N2", CultureInfo.CurrentCulture), _records.Sum(x => x.Deductions).ToString("N2", CultureInfo.CurrentCulture), _records.Sum(x => x.Additions).ToString("N2", CultureInfo.CurrentCulture), _records.Sum(x => x.InsuranceFunds).ToString("N2", CultureInfo.CurrentCulture), _records.Sum(x => x.AdvanceDeduction).ToString("N2", CultureInfo.CurrentCulture), _records.Sum(x => x.AmountPaid).ToString("N2", CultureInfo.CurrentCulture), ""]);
        using var dialog = new SaveFileDialog { Title = "تصدير رواتب Excel", Filter = "ملف Excel (*.xlsx)|*.xlsx", DefaultExt = "xlsx", AddExtension = true, FileName = $"رواتب_{DateTime.Now:yyyyMMdd_HHmm}.xlsx", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var result = await _excel.ExportAsync(new ExcelExportData("الرواتب", headers, rows, "تقرير الرواتب", description), dialog.FileName);
        ResultPresenter.Present(this, "تصدير الرواتب", result);
    }

    private void BeginEdit()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not Guid id)
        { AppDialog.ShowInformation(this, "الرواتب", "اختر سجل راتب لتعديله."); return; }
        var record = _records.FirstOrDefault(x => x.Id == id);
        if (record is null) { AppDialog.ShowInformation(this, "الرواتب", "تعذر العثور على سجل الراتب."); return; }
        if (record.IsPaid) { AppDialog.ShowInformation(this, "الرواتب", "لا يمكن تعديل راتب تم صرفه."); return; }
        _editingPayrollId = id; _grid.Enabled = false;
        _editingEmployeeName.Text = record.EmployeeName;
        _editingEmployeeName.Visible = true;
        _employee.Visible = false;
        _employee.Enabled = false;
        _from.Value = record.PeriodStart.ToDateTime(TimeOnly.MinValue); _to.Value = record.PeriodEnd.ToDateTime(TimeOnly.MinValue);
        _salary.Value = Math.Min(_salary.Maximum, record.AgreedSalary); _additions.Value = Math.Min(_additions.Maximum, record.Additions);
        _deductions.Value = Math.Min(_deductions.Maximum, record.Deductions); _insurance.Value = Math.Min(_insurance.Maximum, record.InsuranceFunds);
        _paymentDate.Value = (record.PaymentDate ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue);
        _notes.Text = record.Notes ?? string.Empty; _savePayroll.Text = "حفظ التعديل وإعادة الحساب";
    }

    private void EndEdit()
    {
        _editingPayrollId = null; _grid.Enabled = true; _savePayroll.Text = "إنشاء وحساب الراتب";
        _employee.Visible = true;
        _employee.Enabled = true;
        _editingEmployeeName.Visible = false;
        if (_employee.SelectedItem is EmployeeDto employee) _salary.Value = Math.Min(_salary.Maximum, employee.Salary);
        _additions.Value = 0; _deductions.Value = 0; _insurance.Value = 0; _notes.Clear();
        var today = DateTime.Today; _from.Value = new DateTime(today.Year, today.Month, 1);
        _to.Value = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        _paymentDate.Value = today;
    }

    private async Task SaveAndCalculateAsync()
    {
        var employee = _employee.SelectedItem as EmployeeDto;
        if (_editingPayrollId is null && employee is null) { AppDialog.ShowInformation(this, "الرواتب", "اختر الموظف أولًا."); return; }
        if (_from.Value.Date > _to.Value.Date) { AppDialog.ShowInformation(this, "الرواتب", "بداية الفترة يجب أن تسبق نهايتها."); return; }
        await UiGuard.RunAsync(this, "Payroll.SaveAndCalculate", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IPayRollRecordService>();
            var periodStart = DateOnly.FromDateTime(_from.Value); var periodEnd = DateOnly.FromDateTime(_to.Value);
            var result = _editingPayrollId is Guid editingId
                ? await service.UpdatePayRollRecordAsync(editingId, new UpdatePayrollRecordDto
                {
                    PeriodStart = periodStart, PeriodEnd = periodEnd, AgreedSalary = _salary.Value,
                    Additions = _additions.Value, Deductions = _deductions.Value, InsuranceFunds = _insurance.Value,
                    PaymentDate = DateOnly.FromDateTime(_paymentDate.Value), Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
                })
                : await service.CreatePayRollRecord(new CreatePayrollRecordDto
                {
                    EmployeeId = employee!.Id, PeriodStart = periodStart, PeriodEnd = periodEnd, AgreedSalary = _salary.Value,
                    Additions = _additions.Value, Deductions = _deductions.Value, InsuranceFunds = _insurance.Value,
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
        }, "تعذر حفظ وحساب الراتب.");
    }

    private async Task WithSelectedAsync(Func<IPayRollRecordService, Guid, Task<Result<PayrollRecordDto>>> action, string success)
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not Guid payrollId)
        { AppDialog.ShowInformation(this, "الرواتب", "اختر سجل راتب."); return; }
        await UiGuard.RunAsync(this, "Payroll.WithSelected", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var result = await action(scope.ServiceProvider.GetRequiredService<IPayRollRecordService>(), payrollId);
            if (result.IsFailure) { AppDialog.ShowInformation(this, "الرواتب", result.Error.Description); return; }
            await LoadAsync(); AppDialog.ShowInformation(this, "الرواتب", success);
        }, "تعذر تنفيذ العملية على سجل الراتب.");
    }

    private async Task PaySelectedAsync()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not Guid id)
        { AppDialog.ShowInformation(this, "الرواتب", "اختر سجل راتب."); return; }
        var record = _records.FirstOrDefault(x => x.Id == id);
        if (record is null) return;
        if (!record.IsCalculated)
        { AppDialog.ShowInformation(this, "الرواتب", "احسب الراتب أولًا قبل الصرف."); return; }
        var advanceDeduction = record.AdvanceDeduction;
        if (record.AdvanceDeduction > 0)
        {
            var selectedDeduction = ChooseAdvanceDeduction(record);
            if (selectedDeduction is null) return;
            advanceDeduction = selectedDeduction.Value;
        }
        var netAmount = record.AmountPaid + record.AdvanceDeduction - advanceDeduction;
        var warning = advanceDeduction > 0
            ? $"سيتم خصم {advanceDeduction:N2} جنيه من السلفة.\nصافي المبلغ الذي سيُصرف: {netAmount:N2} جنيه.\nأي رصيد متبقٍ من السلفة يظل مستحقًا ويُرحّل للشهر التالي. هل تريد تسجيل الصرف؟"
            : $"لن يتم خصم مبلغ من السلفة الآن.\nصافي المبلغ الذي سيُصرف: {netAmount:N2} جنيه، ورصيد السلفة يظل مستحقًا للشهر التالي. هل تريد تسجيل الصرف؟";
        if (!AppDialog.Confirm(this, "تأكيد صرف الراتب", warning)) return;
        await UiGuard.RunAsync(this, "Payroll.PaySelected", async () =>
        {
            await WithSelectedAsync((service, payrollId) => service.MarkRecordAsPaid(payrollId, advanceDeduction), "تم صرف الراتب وتسجيل المصروف.");
        }, "تعذر صرف الراتب.");
    }

    private decimal? ChooseAdvanceDeduction(PayrollRecordDto record)
    {
        using var dialog = new Form
        {
            Text = "خصم السلفة عند صرف الراتب",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ShowIcon = false,
            ClientSize = new Size(500, 220),
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = true,
            Font = new Font("Segoe UI", 10F)
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 3, RightToLeft = RightToLeft.Yes };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        var explanation = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Text = $"اختر مبلغ السلفة الذي تريد خصمه الآن (حتى {record.AdvanceDeduction:N2} جنيه). المبلغ المتبقي سيظل على الموظف للشهر التالي."
        };
        var amount = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = record.AdvanceDeduction,
            Value = record.AdvanceDeduction,
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            TextAlign = HorizontalAlignment.Center
        };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, RightToLeft = RightToLeft.Yes, WrapContents = false };
        var accept = new Button { Text = "متابعة", Width = 110, Height = 36, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "إلغاء", Width = 100, Height = 36, DialogResult = DialogResult.Cancel };
        buttons.Controls.AddRange([accept, cancel]);
        layout.Controls.Add(explanation, 0, 0);
        layout.Controls.Add(amount, 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        dialog.Controls.Add(layout);
        dialog.AcceptButton = accept;
        dialog.CancelButton = cancel;
        return dialog.ShowDialog(this) == DialogResult.OK ? amount.Value : null;
    }

    private void SetEmployeeForSelected()
    {
        if (_editingPayrollId is not null) return;
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not Guid payrollId) return;
        var record = _records.FirstOrDefault(x => x.Id == payrollId);
        if (record is null) return;
        _employee.SelectedItem = _employee.Items.Cast<EmployeeDto>().FirstOrDefault(x => x.Id == record.EmployeeId);
    }
    private static void AddField(Control parent, string label, Control input) { var panel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, RightToLeft = RightToLeft.Yes, Margin = new Padding(5) }; panel.Controls.Add(new Label { Text = label, AutoSize = true }); panel.Controls.Add(input); parent.Controls.Add(panel); }
    private static Button Button(string text) => new() { Text = text, AutoSize = true, Height = 38, Margin = new Padding(5, 22, 5, 4) };
    private static NumericUpDown MoneyInput() => new() { Minimum = 0, Maximum = 100000000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 130 };
}
