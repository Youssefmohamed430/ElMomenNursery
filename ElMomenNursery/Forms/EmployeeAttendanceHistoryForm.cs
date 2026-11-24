using System.Drawing;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

internal sealed class EmployeeAttendanceHistoryForm : Form
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IExcelExportService _excelExportService;
    private readonly DateTimePicker _start = DatePicker();
    private readonly DateTimePicker _end = DatePicker();
    private readonly TextBox _search = new() { Width = 220, PlaceholderText = "اكتب اسم الموظف", RightToLeft = RightToLeft.Yes };
    private readonly ComboBox _employees = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList, RightToLeft = RightToLeft.Yes };
    private readonly Button _show = new() { Text = "عرض السجل", AutoSize = true, Height = 36 };
    private readonly Button _export = new() { Text = "تصدير Excel", AutoSize = true, Height = 36 };
    private readonly Label _summary = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
    private readonly DataGridView _grid = MakeGrid();
    private List<AttendanceEmployeeOptionDto> _allEmployees = [];
    private bool _updatingEmployees;
    private int _loadVersion;

    public EmployeeAttendanceHistoryForm(IServiceScopeFactory scopes, IExcelExportService excelExportService, bool canExport)
    {
        _scopes = scopes;
        _excelExportService = excelExportService;
        Text = "سجل حضور وغياب موظف خلال فترة";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(850, 600);
        Size = new Size(1050, 720);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = BrandPalette.AppBackground;
        Font = new Font("Segoe UI", 10F);

        var filters = new FilterCard { Name = "employee-attendance-history.filters", Dock = DockStyle.Top };
        filters.AddGroup("history.period", "الفترة من / إلى", 2, _start, _end);
        filters.AddField("history.search", "بحث باسم الموظف", _search, 2);
        filters.AddField("history.employee", "اختر الموظف", _employees, 2);
        filters.AddActions(_show);
        var toolbar = new ActionToolbar { Dock = DockStyle.Top, Height = FilterTheme.ToolbarHeight };
        toolbar.Add(_export);
        _export.Visible = canExport;
        _export.Enabled = false;
        _export.Click += async (_, _) => await GridExcelExport.SaveAsync(this, _grid, _excelExportService,
            "تصدير كشف حضور الموظف", "كشف حضور موظف");

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10), RightToLeft = RightToLeft.Yes };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, FilterTheme.ToolbarHeight + 8));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var history = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        history.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        history.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        history.Controls.Add(_summary, 0, 0);
        history.Controls.Add(_grid, 0, 1);
        layout.Controls.Add(filters, 0, 0);
        layout.Controls.Add(toolbar, 0, 1);
        layout.Controls.Add(history, 0, 2);
        Controls.Add(layout);
        layout.Resize += (_, _) => { filters.SetBounds(0, 0, layout.ClientSize.Width, filters.Height); filters.Arrange(); toolbar.SetBounds(0, filters.Bottom + 6, layout.ClientSize.Width, FilterTheme.ToolbarHeight); };
        filters.SetBounds(0, 0, layout.ClientSize.Width, filters.Height); filters.Arrange(); toolbar.SetBounds(0, filters.Bottom + 6, layout.ClientSize.Width, FilterTheme.ToolbarHeight);

        _start.ValueChanged += (_, _) => ClampEndDate();
        _search.TextChanged += (_, _) => FilterEmployees();
        _show.Click += async (_, _) => await LoadHistoryAsync();
        Shown += async (_, _) => await LoadEmployeesAsync();
        AcceptButton = _show;
    }

    private async Task LoadEmployeesAsync()
    {
        await UiGuard.RunAsync(this, "EmployeeAttendanceHistory.LoadEmployees", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IEmployeeAttendanceService>();
            var result = await service.GetEmployeesForHistorySelectionAsync();
            if (IsDisposed) return;
            if (result.IsFailure) { ShowError(result.Error.Description); return; }
            _allEmployees = result.Value.ToList();
            FilterEmployees();
        }, "تعذر تحميل قائمة الموظفين.");
    }

    private void FilterEmployees()
    {
        if (IsDisposed || _updatingEmployees) return;
        var query = ArabicTextNormalizer.Normalize(_search.Text);
        var matches = _allEmployees.Where(x => query.Length == 0
            || ArabicTextNormalizer.Normalize(x.FullName + " " + x.JobDesignation).Contains(query, StringComparison.Ordinal))
            .Take(100).ToArray();
        _updatingEmployees = true;
        try
        {
            _employees.BeginUpdate();
            _employees.Items.Clear();
            foreach (var employee in matches) _employees.Items.Add(new EmployeeOption(employee.Id, employee.FullName, employee.JobDesignation));
            _employees.SelectedIndex = matches.Length == 1 ? 0 : -1;
        }
        finally { _employees.EndUpdate(); _updatingEmployees = false; }
    }

    private async Task LoadHistoryAsync()
    {
        var selectedEmployee = _employees.SelectedItem as EmployeeOption;
        if (selectedEmployee is null) { ShowError("ابحث عن الموظف ثم اختر اسمه من القائمة."); return; }
        var start = DateOnly.FromDateTime(_start.Value);
        var end = DateOnly.FromDateTime(_end.Value);
        if (start > end) { ShowError("تاريخ البداية يجب أن يكون قبل تاريخ النهاية أو مساويًا له."); return; }
        var version = Interlocked.Increment(ref _loadVersion);
        _show.Enabled = false;
        try
        {
            await UiGuard.RunAsync(this, "EmployeeAttendanceHistory.LoadHistory", async () =>
            {
                await using var scope = _scopes.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IEmployeeAttendanceService>();
                var result = await service.GetEmployeeAttendanceByRangeAsync(selectedEmployee.Id, start, end);
                if (IsDisposed || version != _loadVersion) return;
                if (result.IsFailure) { ShowError(result.Error.Description); return; }
                RenderHistory(selectedEmployee.FullName, result.Value);
            }, "تعذر تحميل سجل حضور الموظف.");
        }
        finally { if (!IsDisposed && version == _loadVersion) _show.Enabled = true; }
    }

    private void RenderHistory(string employeeName, IReadOnlyList<EmployeeAttendanceDto> records)
    {
        _grid.Rows.Clear();
        foreach (var record in records.OrderBy(x => x.AttendanceDate))
            _grid.Rows.Add(employeeName, record.AttendanceDate.ToString("dd/MM/yyyy"), AttendanceLabels.Status(record.Status),
                Time(record.CheckInTime), Time(record.CheckOutTime), AttendanceLabels.Method(record.EntryMethod), record.Notes ?? string.Empty);
        var present = records.Count(x => x.Status is AttendanceStatus.Present or AttendanceStatus.Late);
        var absent = records.Count(x => x.Status is AttendanceStatus.Absent or AttendanceStatus.Excused);
        _export.Enabled = records.Count > 0;
        _summary.Text = $"{employeeName}  |  المسجل: {records.Count}  |  الحضور: {present}  |  الغياب: {absent}";
        if (records.Count == 0) _summary.Text += "  |  لا توجد سجلات في هذه الفترة";
    }

    private void ClampEndDate()
    {
        if (_end.Value.Date < _start.Value.Date) _end.Value = _start.Value.Date;
    }

    private void ShowError(string message)
    {
        if (!IsDisposed) AppDialog.ShowInformation(this, "سجل حضور الموظف", message);
    }

    private static DateTimePicker DatePicker() => new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 145 };
    private static string Time(DateTimeOffset? time) => time?.ToLocalTime().ToString("HH:mm") ?? "—";
    private static DataGridView MakeGrid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RightToLeft = RightToLeft.Yes,
        BackgroundColor = Color.White, RowHeadersVisible = false,
        Columns =
        {
            new DataGridViewTextBoxColumn { HeaderText = "الموظف" },
            new DataGridViewTextBoxColumn { HeaderText = "التاريخ" },
            new DataGridViewTextBoxColumn { HeaderText = "الحالة" },
            new DataGridViewTextBoxColumn { HeaderText = "وقت الحضور" },
            new DataGridViewTextBoxColumn { HeaderText = "وقت الانصراف" },
            new DataGridViewTextBoxColumn { HeaderText = "طريقة التسجيل" },
            new DataGridViewTextBoxColumn { HeaderText = "ملاحظات" }
        }
    };

    private sealed record EmployeeOption(Guid Id, string FullName, string JobDesignation)
    {
        public override string ToString() => $"{FullName} — {JobDesignation}";
    }
}
