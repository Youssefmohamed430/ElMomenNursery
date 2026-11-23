using System.Drawing;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

public sealed class AttendancePage : UserControl
{
    public AttendancePage(IServiceScopeFactory scopes, UserSession session, IExcelExportService excelExportService)
    {
        Dock = DockStyle.Fill;
        RightToLeft = RightToLeft.Yes;
        var tabs = new TabControl { Dock = DockStyle.Fill, RightToLeft = RightToLeft.Yes };
        if (session.HasPermission("Attendance.View"))
            tabs.TabPages.Add(new TabPage("حضور الأطفال") { Controls = { new ChildAttendancePage(scopes, session, excelExportService) } });
        if (session.HasPermission("EmployeeAttendance.View"))
            tabs.TabPages.Add(new TabPage("حضور الموظفين") { Controls = { new EmployeeAttendancePage(scopes, session, excelExportService) } });
        Controls.Add(tabs);
    }
}

internal sealed class ChildAttendancePage : UserControl
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IExcelExportService _excelExportService;
    private readonly DateTimePicker _date = FilterTheme.CreateDatePicker();
    private readonly ComboBox _classroom = FilterTheme.CreateComboBox();
    private readonly ComboBox _status = FilterTheme.CreateComboBox();
    private readonly FilterSearchBox _search = new("بحث باسم الطفل");
    private readonly DataGridView _grid = MakeGrid();
    private readonly Label _summary = new() { AutoSize = false, Height = 34, TextAlign = ContentAlignment.MiddleRight };
    private readonly Button _present = MakeButton("حاضر الآن");
    private readonly Button _absent = MakeButton("غائب");
    private readonly Button _checkout = MakeButton("تسجيل انصراف");
    private readonly Button _manual = MakeButton("تسجيل يدوي");
    private readonly Button _correct = MakeButton("تصحيح السجل");
    private readonly Button _markAll = MakeButton("تسجيل الكل حاضر", true);
    private readonly Button _refresh = MakeButton("تحديث");
    private readonly Button _history = MakeButton("سجل طفل لفترة");
    private List<ChildDailyAttendanceDto> _rows = [];
    private int _loadVersion;

    public ChildAttendancePage(IServiceScopeFactory scopes, UserSession session, IExcelExportService excelExportService)
    {
        _scopes = scopes; _excelExportService = excelExportService;
        Dock = DockStyle.Fill; RightToLeft = RightToLeft.Yes; BackColor = BrandPalette.AppBackground;
        _date.MaxDate = DateTime.Today;
        _status.Items.AddRange(["الكل", "لم يسجل", "حاضر", "غائب", "متأخر", "غياب بعذر"]); _status.SelectedIndex = 0;
        _classroom.Items.Add(new ClassroomFilterOptionDto(Guid.Empty, "كل الفصول")); _classroom.SelectedIndex = 0;
        _markAll.Visible = session.HasPermission("Attendance.Record");
        _present.Visible = _absent.Visible = _checkout.Visible = session.HasPermission("Attendance.Record");
        _manual.Visible = session.HasPermission("Attendance.Record"); _correct.Visible = session.HasPermission("Attendance.Edit");
        _history.Visible = session.HasPermission("Attendance.View");
        var export = FilterTheme.CreateSecondaryButton("تصدير Excel");
        export.AutoSize = true;
        export.Height = FilterTheme.ToolbarHeight;
        export.Visible = session.HasPermission("Attendance.Export");
        export.Click += async (_, _) => await GridExcelExport.SaveAsync((IWin32Window?)FindForm() ?? this, _grid,
            _excelExportService, "تصدير حضور الأطفال", "حضور الأطفال");
        export.Name = "attendance.export";

        var filters = new FilterCard { Name = "attendance.filters" };
        filters.AddField("attendance.date", "التاريخ", _date);
        filters.AddField("attendance.classroom", "الفصل", _classroom);
        filters.AddField("attendance.status", "الحالة", _status);
        filters.AddField("attendance.search", "بحث", _search, columnSpan: 3);

        var toolbar = new ActionToolbar { Name = "attendance.actions" };
        toolbar.Add(_refresh, _history, _manual, _present, _absent, _checkout, _correct, _markAll, export);

        _grid.Dock = DockStyle.None;
        _summary.Dock = DockStyle.None;
        Controls.Add(_grid);
        Controls.Add(_summary);
        Controls.Add(toolbar);
        Controls.Add(filters);
        toolbar.LayoutChanged += (_, _) => LayoutContent();
        VisibleChanged += (_, _) => { if (Visible) LayoutContent(); };
        Resize += (_, _) => LayoutContent();
        LayoutContent();

        void LayoutContent()
        {
            const int margin = 14;
            var width = Math.Max(240, ClientSize.Width - margin * 2);
            filters.SetBounds(margin, margin, width, filters.Height);
            filters.Arrange();
            var toolbarTop = filters.Bottom + 10;
            toolbar.SetBounds(margin, toolbarTop, width, toolbar.ArrangeItems(width));
            _summary.SetBounds(margin, toolbar.Bottom + 4, width, 34);
            _grid.SetBounds(margin, _summary.Bottom + 4, width, Math.Max(120, ClientSize.Height - _summary.Bottom - margin));
        }

        _date.ValueChanged += async (_, _) => await LoadAsync(); _classroom.SelectedIndexChanged += async (_, _) => await LoadAsync();
        _search.Input.TextChanged += (_, _) => Render(); _status.SelectedIndexChanged += (_, _) => Render(); _refresh.Click += async (_, _) => await LoadAsync();
        _present.Click += async (_, _) => await RecordAsync(AttendanceStatus.Present);
        _absent.Click += async (_, _) => await RecordAsync(AttendanceStatus.Absent);
        _checkout.Click += async (_, _) => await CheckOutAsync(); _markAll.Click += async (_, _) => await MarkAllAsync();
        _manual.Click += async (_, _) => await OpenEditorAsync(correction: false);
        _correct.Click += async (_, _) => await OpenEditorAsync(correction: true);
        _history.Click += (_, _) =>
        {
            using var form = new ChildAttendanceHistoryForm(_scopes, _excelExportService, session.HasPermission("Attendance.Export"));
            form.ShowDialog(FindForm());
        };
        _grid.SelectionChanged += (_, _) => UpdateActions(); _grid.CellDoubleClick += async (_, _) => await OpenEditorAsync(correction: Selected()?.IsMarked == true);
        _grid.KeyDown += async (_, e) => { if (e.KeyCode == Keys.F5) { e.Handled = true; await LoadAsync(); } };
        Load += async (_, _) => { await LoadClassroomsAsync(); await LoadAsync(); };
    }

    private async Task LoadClassroomsAsync()
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IChildService>();
            var result = await service.GetClassroomsForFilterAsync();
            if (IsDisposed) return;
            if (result.IsFailure) { ShowError(result.Error.Description); return; }
            _classroom.Items.Clear(); _classroom.Items.Add(new ClassroomFilterOptionDto(Guid.Empty, "كل الفصول"));
            foreach (var value in result.Value) _classroom.Items.Add(value);
            _classroom.SelectedIndex = 0;
        }
        catch (UnauthorizedAccessException) { ShowError("لا تملك الصلاحية اللازمة لعرض الفصول."); }
        catch { ShowError("تعذر تحميل قائمة الفصول."); }
    }

    private async Task LoadAsync()
    {
        var version = Interlocked.Increment(ref _loadVersion); var selected = SelectedId();
        try
        {
            var classId = (_classroom.SelectedItem as ClassroomFilterOptionDto)?.Id;
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IChildAttendanceService>();
            var result = await service.GetDailyAttendanceAsync(new ChildDailyAttendanceQuery(DateOnly.FromDateTime(_date.Value), classId == Guid.Empty ? null : classId));
            if (IsDisposed || version != _loadVersion) return;
            if (result.IsFailure) { ShowError(result.Error.Description); return; }
            _rows = result.Value.ToList(); Render(); RestoreSelection(selected);
        }
        catch (Exception) { if (!IsDisposed && version == _loadVersion) ShowError("تعذر تحميل حضور الأطفال."); }
    }

    private void Render()
    {
        if (IsDisposed) return;
        var query = ArabicTextNormalizer.Normalize(_search.Text);
        var state = _status.SelectedItem?.ToString() ?? "الكل";
        var visible = _rows.Where(row => (query.Length == 0 || ArabicTextNormalizer.Normalize(row.ChildName).Contains(query, StringComparison.Ordinal))
            && (state == "الكل" || state == "لم يسجل" && !row.IsMarked || state == "حاضر" && row.Status == AttendanceStatus.Present
                || state == "غائب" && row.Status == AttendanceStatus.Absent || state == "متأخر" && row.Status == AttendanceStatus.Late
                || state == "غياب بعذر" && row.Status == AttendanceStatus.Excused)).ToArray();
        _grid.Rows.Clear();
        foreach (var row in visible)
        {
            var index = _grid.Rows.Add(row.ChildName, row.ClassroomNames, row.Status is null ? "لم يسجل" : AttendanceLabels.Status(row.Status.Value),
                Time(row.CheckIn), Time(row.CheckOut), row.EntryMethod is null ? "—" : AttendanceLabels.Method(row.EntryMethod.Value), row.Notes ?? "");
            _grid.Rows[index].Tag = row;
        }
        var total = visible.Length;
        _summary.Text = $"الإجمالي: {total}  |  حاضر: {visible.Count(x => x.Status == AttendanceStatus.Present)}  |  غائب: {visible.Count(x => x.Status == AttendanceStatus.Absent)}  |  متأخر: {visible.Count(x => x.Status == AttendanceStatus.Late)}  |  بعذر: {visible.Count(x => x.Status == AttendanceStatus.Excused)}  |  لم يسجل: {visible.Count(x => !x.IsMarked)}";
        UpdateActions();
    }

    private async Task RecordAsync(AttendanceStatus status)
    {
        var row = Selected(); if (row is null || row.IsMarked) return;
        var now = DateTimeOffset.Now;
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IChildAttendanceService>();
            var result = await service.MarkManualAttendanceAsync(new CreateChildAttendanceDto(row.ChildId, row.Date, status,
                status is AttendanceStatus.Present or AttendanceStatus.Late ? now : null));
            if (result.IsFailure) { ShowError(result.Error.Description); return; }
            if (!string.IsNullOrWhiteSpace(result.Value.Alert)) ShowError(result.Value.Alert);
            await LoadAsync();
        }
        catch { if (!IsDisposed) ShowError("تعذر تسجيل الحضور."); }
    }

    private async Task CheckOutAsync()
    {
        var row = Selected(); if (row is null) return;
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IChildAttendanceService>();
            var result = await service.RecordCheckOutAsync(row.ChildId, row.Date, DateTimeOffset.Now);
            if (result.IsFailure) { ShowError(result.Error.Description); return; }
            await LoadAsync();
        }
        catch { if (!IsDisposed) ShowError("تعذر تسجيل الانصراف."); }
    }

    private async Task MarkAllAsync()
    {
        var classroom = _classroom.SelectedItem as ClassroomFilterOptionDto;
        var candidates = _rows.Count(x => !x.IsMarked);
        if (candidates == 0) { ShowError("لا يوجد أطفال غير مسجلين في النطاق المحدد."); return; }
        var className = classroom?.Id == Guid.Empty ? "كل الفصول" : classroom?.Name ?? "كل الفصول";
        if (!AppDialog.Confirm(this, "تسجيل الكل حاضر", $"سيتم تسجيل {candidates} طفلًا بحالة حاضر في {className}. هل تريد المتابعة؟")) return;
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IChildAttendanceService>();
            var result = await service.MarkAllPresentAsync(DateOnly.FromDateTime(_date.Value), classroom?.Id == Guid.Empty ? null : classroom?.Id);
            if (result.IsFailure) { ShowError(result.Error.Description); return; }
            await LoadAsync();
        }
        catch { if (!IsDisposed) ShowError("تعذر تسجيل الحضور الجماعي."); }
    }

    private async Task OpenEditorAsync(bool correction)
    {
        var row = Selected(); if (row is null || correction != row.IsMarked) return;
        using var editor = new AttendanceEditorForm("الطفل", row.ChildName, row.Date, correction, row.Status,
            row.CheckIn, row.CheckOut, row.Notes, async values =>
            {
                await using var scope = _scopes.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IChildAttendanceService>();
                if (row.AttendanceId is Guid id)
                {
                    var updated = await service.UpdateChildAttendanceAsync(id, new UpdateChildAttendanceDto(values.Status, values.CheckIn, values.CheckOut, values.Notes, values.Reason));
                    return updated.IsFailure ? updated.Error : null;
                }
                var created = await service.MarkManualAttendanceAsync(new CreateChildAttendanceDto(row.ChildId, row.Date, values.Status, values.CheckIn, values.CheckOut, values.Notes));
                if (created.IsSuccess && !string.IsNullOrWhiteSpace(created.Value.Alert)) ShowError(created.Value.Alert);
                return created.IsFailure ? created.Error : null;
            });
        if (editor.ShowDialog(FindForm()) == DialogResult.OK && !IsDisposed) await LoadAsync();
    }

    private ChildDailyAttendanceDto? Selected() => _grid.SelectedRows.Count == 0 ? null : _grid.SelectedRows[0].Tag as ChildDailyAttendanceDto;
    private Guid? SelectedId() => Selected()?.ChildId;
    private void RestoreSelection(Guid? id) { if (id is null) return; foreach (DataGridViewRow row in _grid.Rows) if ((row.Tag as ChildDailyAttendanceDto)?.ChildId == id) { row.Selected = true; _grid.CurrentCell = row.Cells[0]; break; } }
    private void UpdateActions() { var row = Selected(); _present.Enabled = _absent.Enabled = _manual.Enabled = row is { IsMarked: false }; _correct.Enabled = _correct.Visible && row is { IsMarked: true }; _checkout.Enabled = _checkout.Visible && row is { IsMarked: true, CheckIn: not null, CheckOut: null }; }
    private void ShowError(string message) { if (!IsDisposed) AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "حضور الأطفال", message); }
    private static string Time(DateTimeOffset? time) => time?.ToLocalTime().ToString("HH:mm") ?? "—";
    private static Button MakeButton(string text, bool primary = false)
    {
        var button = BrandControls.ActionButton(text, primary, autoSize: true);
        button.Height = FilterTheme.ToolbarHeight;
        button.MinimumSize = new Size(0, FilterTheme.ToolbarHeight);
        return button;
    }
    private static DataGridView MakeGrid() => new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RightToLeft = RightToLeft.Yes, BackgroundColor = Color.White, RowHeadersVisible = false, Columns = { new DataGridViewTextBoxColumn { HeaderText = "الطفل" }, new DataGridViewTextBoxColumn { HeaderText = "الفصل" }, new DataGridViewTextBoxColumn { HeaderText = "الحالة" }, new DataGridViewTextBoxColumn { HeaderText = "وقت الحضور" }, new DataGridViewTextBoxColumn { HeaderText = "وقت الانصراف" }, new DataGridViewTextBoxColumn { HeaderText = "طريقة التسجيل" }, new DataGridViewTextBoxColumn { HeaderText = "ملاحظات" } } };
}

internal sealed class EmployeeAttendancePage : UserControl
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IExcelExportService _excelExportService;
    private readonly DateTimePicker _date = FilterTheme.CreateDatePicker();
    private readonly FilterSearchBox _search = new("بحث باسم الموظف أو الوظيفة");
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RightToLeft = RightToLeft.Yes, RowHeadersVisible = false, Columns = { new DataGridViewTextBoxColumn { HeaderText = "الموظف" }, new DataGridViewTextBoxColumn { HeaderText = "الوظيفة" }, new DataGridViewTextBoxColumn { HeaderText = "الحالة" }, new DataGridViewTextBoxColumn { HeaderText = "الحضور" }, new DataGridViewTextBoxColumn { HeaderText = "الانصراف" }, new DataGridViewTextBoxColumn { HeaderText = "طريقة التسجيل" }, new DataGridViewTextBoxColumn { HeaderText = "ملاحظات" } } };
    private readonly Button _present = MakeButton("حاضر الآن");
    private readonly Button _absent = MakeButton("غائب");
    private readonly Button _checkout = MakeButton("تسجيل انصراف");
    private readonly Button _edit = MakeButton("تصحيح السجل");
    private readonly Button _refresh = MakeButton("تحديث");
    private readonly Button _export = MakeButton("تصدير Excel");
    private readonly Button _history = MakeButton("سجل موظف لفترة");
    private readonly Label _summary = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
    private List<EmployeeDailyAttendanceDto> _rows = [];
    private int _version;

    public EmployeeAttendancePage(IServiceScopeFactory scopes, UserSession session, IExcelExportService excelExportService)
    {
        _scopes = scopes; _excelExportService = excelExportService; Dock = DockStyle.Fill; BackColor = BrandPalette.AppBackground; RightToLeft = RightToLeft.Yes;
        _date.MaxDate = DateTime.Today;
        _export.Visible = session.HasPermission("EmployeeAttendance.Export");
        _export.Click += async (_, _) => await GridExcelExport.SaveAsync((IWin32Window?)FindForm() ?? this, _grid,
            _excelExportService, "تصدير حضور الموظفين", "حضور الموظفين");
        _history.Visible = session.HasPermission("EmployeeAttendance.View");
        _history.Click += (_, _) =>
        {
            using var form = new EmployeeAttendanceHistoryForm(_scopes, _excelExportService, session.HasPermission("EmployeeAttendance.Export"));
            form.ShowDialog(FindForm());
        };
        _present.Visible = _absent.Visible = _checkout.Visible = session.HasPermission("EmployeeAttendance.Record");
        _edit.Visible = session.HasPermission("EmployeeAttendance.Edit");

        var filters = new FilterCard { Name = "employee-attendance.filters" };
        filters.AddField("employee-attendance.date", "التاريخ", _date);
        filters.AddField("employee-attendance.search", "بحث", _search, columnSpan: 3);

        var toolbar = new ActionToolbar { Name = "employee-attendance.actions" };
        toolbar.Add(_refresh, _history, _present, _absent, _checkout, _edit, _export);

        _grid.Dock = DockStyle.None;
        _summary.Dock = DockStyle.None;
        Controls.Add(_grid);
        Controls.Add(_summary);
        Controls.Add(toolbar);
        Controls.Add(filters);
        toolbar.LayoutChanged += (_, _) => LayoutContent();
        VisibleChanged += (_, _) => { if (Visible) LayoutContent(); };
        Resize += (_, _) => LayoutContent();
        LayoutContent();

        void LayoutContent()
        {
            const int margin = 12;
            var width = Math.Max(240, ClientSize.Width - margin * 2);
            filters.SetBounds(margin, margin, width, filters.Height);
            filters.Arrange();
            var toolbarTop = filters.Bottom + 10;
            toolbar.SetBounds(margin, toolbarTop, width, toolbar.ArrangeItems(width));
            _summary.SetBounds(margin, toolbar.Bottom + 4, width, 30);
            _grid.SetBounds(margin, _summary.Bottom + 4, width, Math.Max(120, ClientSize.Height - _summary.Bottom - margin));
        }

        _date.ValueChanged += async (_, _) => await LoadAsync(); _refresh.Click += async (_, _) => await LoadAsync(); _search.Input.TextChanged += (_, _) => Render();
        _grid.SelectionChanged += (_, _) => UpdateActions(); _present.Click += async (_, _) => await RecordAsync(AttendanceStatus.Present); _absent.Click += async (_, _) => await RecordAsync(AttendanceStatus.Absent);
        _checkout.Click += async (_, _) => await CheckoutAsync(); _edit.Click += async (_, _) => await OpenEditorAsync();
        _grid.CellDoubleClick += async (_, _) => { if (Current() is not null) await OpenEditorAsync(); };
        _grid.KeyDown += async (_, e) => { if (e.KeyCode == Keys.F5) { e.Handled = true; await LoadAsync(); } };
        Load += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var version = Interlocked.Increment(ref _version); var selected = Current()?.EmployeeId;
        try
        {
            await using var scope = _scopes.CreateAsyncScope(); var service = scope.ServiceProvider.GetRequiredService<IEmployeeAttendanceService>();
            var result = await service.GetDailyAttendanceAsync(DateOnly.FromDateTime(_date.Value));
            if (result.IsFailure) { AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "حضور الموظفين", result.Error.Description); return; }
            _rows = result.Value.ToList();
            if (IsDisposed || version != _version) return; Render();
            if (selected is Guid id) foreach (DataGridViewRow row in _grid.Rows) if ((row.Tag as EmployeeDailyAttendanceDto)?.EmployeeId == id) { row.Selected = true; break; }
        }
        catch (UnauthorizedAccessException) { if (!IsDisposed) AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "حضور الموظفين", "لا تملك الصلاحية اللازمة لعرض حضور الموظفين."); }
        catch { if (!IsDisposed && version == _version) AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "حضور الموظفين", "تعذر تحميل حضور الموظفين."); }
    }

    private void Render()
    {
        var query = ArabicTextNormalizer.Normalize(_search.Text);
        var rows = _rows.Where(x => query.Length == 0 || ArabicTextNormalizer.Normalize(x.EmployeeName + " " + x.JobDesignation).Contains(query, StringComparison.Ordinal)).ToArray();
        _grid.Rows.Clear(); foreach (var row in rows) { var i = _grid.Rows.Add(row.EmployeeName, row.JobDesignation, row.Status is null ? "لم يسجل" : AttendanceLabels.Status(row.Status.Value), row.CheckInTime?.ToLocalTime().ToString("HH:mm") ?? "—", row.CheckOutTime?.ToLocalTime().ToString("HH:mm") ?? "—", row.EntryMethod is null ? "—" : AttendanceLabels.Method(row.EntryMethod.Value), row.Notes ?? ""); _grid.Rows[i].Tag = row; }
        _summary.Text = $"الإجمالي: {rows.Length}  |  حاضر: {rows.Count(x => x.Status == AttendanceStatus.Present)}  |  غائب: {rows.Count(x => x.Status == AttendanceStatus.Absent)}  |  متأخر: {rows.Count(x => x.Status == AttendanceStatus.Late)}  |  بعذر: {rows.Count(x => x.Status == AttendanceStatus.Excused)}  |  لم يسجل: {rows.Count(x => !x.IsMarked)}";
        UpdateActions();
    }

    private async Task RecordAsync(AttendanceStatus status)
    {
        var row = Current(); if (row is null || row.IsMarked) return;
        try { await using var scope = _scopes.CreateAsyncScope(); var service = scope.ServiceProvider.GetRequiredService<IEmployeeAttendanceService>();
            var result = await service.MarkManualAttendanceAsync(new CreateEmployeeAttendanceDto { EmployeeId = row.EmployeeId, AttendanceDate = row.AttendanceDate, Status = status, CheckInTime = status is AttendanceStatus.Present or AttendanceStatus.Late ? DateTimeOffset.Now : null });
            if (result.IsFailure) { AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "حضور الموظفين", result.Error.Description); return; }
            await LoadAsync(); }
        catch { if (!IsDisposed) AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "حضور الموظفين", "تعذر تسجيل الحضور."); }
    }
    private async Task CheckoutAsync()
    {
        var row = Current(); if (row is null) return;
        try { await using var scope = _scopes.CreateAsyncScope(); var service = scope.ServiceProvider.GetRequiredService<IEmployeeAttendanceService>();
            var result = await service.RecordCheckOutAsync(row.EmployeeId, row.AttendanceDate, DateTimeOffset.Now);
            if (result.IsFailure) { AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "حضور الموظفين", result.Error.Description); return; }
            await LoadAsync(); }
        catch { if (!IsDisposed) AppDialog.ShowInformation((IWin32Window?)FindForm() ?? this, "حضور الموظفين", "تعذر تسجيل الانصراف."); }
    }
    private async Task OpenEditorAsync()
    {
        var row = Current(); if (row is null) return;
        if (row.IsMarked && !_sessionHasEdit || !row.IsMarked && !_sessionHasRecord) return;
        using var editor = new AttendanceEditorForm("الموظف", row.EmployeeName, row.AttendanceDate, row.IsMarked,
            row.Status, row.CheckInTime, row.CheckOutTime, row.Notes, async values =>
            {
                await using var scope = _scopes.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IEmployeeAttendanceService>();
                var result = row.AttendanceId is Guid attendanceId
                    ? await service.UpdateEmployeeAttendanceAsync(attendanceId, new UpdateEmployeeAttendanceDto { Status = values.Status, CheckInTime = values.CheckIn, CheckOutTime = values.CheckOut, Notes = values.Notes, Reason = values.Reason })
                    : await service.MarkManualAttendanceAsync(new CreateEmployeeAttendanceDto { EmployeeId = row.EmployeeId, AttendanceDate = row.AttendanceDate, Status = values.Status, CheckInTime = values.CheckIn, CheckOutTime = values.CheckOut, Notes = values.Notes });
                return result.IsFailure ? result.Error : null;
            });
        if (editor.ShowDialog(FindForm()) == DialogResult.OK && !IsDisposed) await LoadAsync();
    }
    private EmployeeDailyAttendanceDto? Current() => _grid.SelectedRows.Count == 0 ? null : _grid.SelectedRows[0].Tag as EmployeeDailyAttendanceDto;
    private bool _sessionHasRecord => _present.Visible;
    private bool _sessionHasEdit => _edit.Visible;
    private void UpdateActions() { var row = Current(); _present.Enabled = _absent.Enabled = row is { IsMarked: false }; _edit.Enabled = _edit.Visible && row is { IsMarked: true }; _checkout.Enabled = _checkout.Visible && row is { IsMarked: true, CheckInTime: not null, CheckOutTime: null }; }
    private static Button MakeButton(string text, bool primary = false)
    {
        var button = BrandControls.ActionButton(text, primary, autoSize: true);
        button.Height = FilterTheme.ToolbarHeight;
        button.MinimumSize = new Size(0, FilterTheme.ToolbarHeight);
        return button;
    }
}


