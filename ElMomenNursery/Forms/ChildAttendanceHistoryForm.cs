using System.Drawing;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

internal sealed class ChildAttendanceHistoryForm : Form
{
    private readonly IServiceScopeFactory _scopes;
    private readonly DateTimePicker _start = DatePicker();
    private readonly DateTimePicker _end = DatePicker();
    private readonly TextBox _search = new() { Width = 220, PlaceholderText = "اكتب اسم الطفل" , RightToLeft = RightToLeft.Yes };
    private readonly ComboBox _children = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList, RightToLeft = RightToLeft.Yes };
    private readonly Button _show = new() { Text = "عرض السجل", AutoSize = true, Height = 36 };
    private readonly Label _summary = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
    private readonly DataGridView _grid = MakeGrid();
    private List<AttendanceChildOptionDto> _allChildren = [];
    private bool _updatingChildren;
    private int _loadVersion;

    public ChildAttendanceHistoryForm(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
        Text = "سجل حضور وغياب طفل خلال فترة";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(850, 600);
        Size = new Size(1050, 720);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = BrandPalette.AppBackground;
        Font = new Font("Segoe UI", 10F);

        var dateFilters = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(8) };
        dateFilters.Controls.AddRange([new Label { Text = "من تاريخ", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, _start,
            new Label { Text = "إلى تاريخ", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, _end]);
        var childFilters = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(8) };
        childFilters.Controls.AddRange([new Label { Text = "اسم الطفل", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, _search,
            new Label { Text = "اختر الطفل", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, _children, _show]);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10), RightToLeft = RightToLeft.Yes };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var history = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        history.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        history.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        history.Controls.Add(_summary, 0, 0);
        history.Controls.Add(_grid, 0, 1);
        layout.Controls.Add(dateFilters, 0, 0);
        layout.Controls.Add(childFilters, 0, 1);
        layout.Controls.Add(history, 0, 2);
        Controls.Add(layout);

        _start.ValueChanged += (_, _) => ClampEndDate();
        _search.TextChanged += (_, _) => FilterChildren();
        _show.Click += async (_, _) => await LoadHistoryAsync();
        Shown += async (_, _) => await LoadChildrenAsync();
        AcceptButton = _show;
    }

    private async Task LoadChildrenAsync()
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IChildAttendanceService>();
            var result = await service.GetChildrenForHistorySelectionAsync();
            if (IsDisposed) return;
            if (result.IsFailure) { ShowError(result.Error.Description); return; }
            _allChildren = result.Value.ToList();
            FilterChildren();
        }
        catch { if (!IsDisposed) ShowError("تعذر تحميل قائمة الأطفال."); }
    }

    private void FilterChildren()
    {
        if (IsDisposed || _updatingChildren) return;
        var query = ArabicTextNormalizer.Normalize(_search.Text);
        var matches = _allChildren.Where(x => query.Length == 0 || ArabicTextNormalizer.Normalize(x.FullName).Contains(query, StringComparison.Ordinal)).Take(100).ToArray();
        _updatingChildren = true;
        try
        {
            _children.BeginUpdate();
            _children.Items.Clear();
            foreach (var child in matches) _children.Items.Add(new ChildOption(child.Id, child.FullName));
            _children.SelectedIndex = matches.Length == 1 ? 0 : -1;
        }
        finally { _children.EndUpdate(); _updatingChildren = false; }
    }

    private async Task LoadHistoryAsync()
    {
        var selectedChild = _children.SelectedItem as ChildOption;
        if (selectedChild is null) { ShowError("ابحث عن الطفل ثم اختر اسمه من القائمة."); return; }
        var start = DateOnly.FromDateTime(_start.Value);
        var end = DateOnly.FromDateTime(_end.Value);
        if (start > end) { ShowError("تاريخ البداية يجب أن يكون قبل تاريخ النهاية أو مساويًا له."); return; }
        var version = Interlocked.Increment(ref _loadVersion);
        _show.Enabled = false;
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IChildAttendanceService>();
            var result = await service.GetChildAttendanceByRangeAsync(selectedChild.Id, start, end);
            if (IsDisposed || version != _loadVersion) return;
            if (result.IsFailure) { ShowError(result.Error.Description); return; }
            RenderHistory(selectedChild.FullName, result.Value);
        }
        catch { if (!IsDisposed && version == _loadVersion) ShowError("تعذر تحميل سجل حضور الطفل."); }
        finally { if (!IsDisposed && version == _loadVersion) _show.Enabled = true; }
    }

    private void RenderHistory(string childName, IReadOnlyList<ChildAttendanceDto> records)
    {
        _grid.Rows.Clear();
        foreach (var record in records.OrderBy(x => x.Date))
            _grid.Rows.Add(record.Date.ToString("dd/MM/yyyy"), AttendanceLabels.Status(record.Status), Time(record.CheckIn),
                Time(record.CheckOut), AttendanceLabels.Method(record.EntryMethod), record.Notes ?? string.Empty);
        var present = records.Count(x => x.Status is AttendanceStatus.Present or AttendanceStatus.Late);
        var absent = records.Count(x => x.Status is AttendanceStatus.Absent or AttendanceStatus.Excused);
        _summary.Text = $"{childName}  |  المسجل: {records.Count}  |  الحضور: {present}  |  الغياب: {absent}  |  لم يسجل: لا يظهر كسجل غياب";
        if (records.Count == 0) _summary.Text += "  |  لا توجد سجلات في هذه الفترة";
    }

    private void ClampEndDate()
    {
        if (_end.Value.Date < _start.Value.Date) _end.Value = _start.Value.Date;
    }

    private void ShowError(string message)
    {
        if (!IsDisposed) AppDialog.ShowInformation(this, "سجل حضور الطفل", message);
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
            new DataGridViewTextBoxColumn { HeaderText = "التاريخ" },
            new DataGridViewTextBoxColumn { HeaderText = "الحالة" },
            new DataGridViewTextBoxColumn { HeaderText = "وقت الحضور" },
            new DataGridViewTextBoxColumn { HeaderText = "وقت الانصراف" },
            new DataGridViewTextBoxColumn { HeaderText = "طريقة التسجيل" },
            new DataGridViewTextBoxColumn { HeaderText = "ملاحظات" }
        }
    };

    private sealed record ChildOption(Guid Id, string FullName)
    {
        public override string ToString() => FullName;
    }
}
