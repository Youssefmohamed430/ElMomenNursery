using System.Globalization;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

internal sealed class ChildAttendanceSheetForm : ResponsiveForm
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IExcelExportService _excel;
    private readonly ComboBox _class = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short };
    public ChildAttendanceSheetForm(IServiceScopeFactory scopes, IExcelExportService excel)
    {
        _scopes = scopes; _excel = excel; Text = "كشف حضور فصل"; Width = 470; Height = 290; StartPosition = FormStartPosition.CenterParent; RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
        var nextMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
        _from.Value = nextMonth; _to.Value = nextMonth.AddMonths(1).AddDays(-1);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 4, RightToLeft = RightToLeft.Yes };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Add("الفصل", _class, 0); Add("من تاريخ", _from, 1); Add("إلى تاريخ", _to, 2);
        var export = new Button { Text = "إنشاء الكشف", AutoSize = true }; export.Click += async (_, _) => await ExportAsync(); table.Controls.Add(export, 1, 3); Controls.Add(table);
        Shown += async (_, _) => await LoadClasses();
        void Add(string label, Control control, int row) { table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); table.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, row); control.Dock = DockStyle.Fill; table.Controls.Add(control, 1, row); }
    }
    private async Task LoadClasses()
    {
        await using var s = _scopes.CreateAsyncScope(); var result = await s.ServiceProvider.GetRequiredService<IChildAttendanceService>().GetClassroomsForFilterAsync();
        if (result.IsFailure) { AppDialog.ShowInformation(this, Text, result.Error.Description); return; }
        foreach (var room in result.Value) _class.Items.Add(room); if (_class.Items.Count > 0) _class.SelectedIndex = 0;
    }
    private async Task ExportAsync()
    {
        if (_class.SelectedItem is not ClassroomFilterOptionDto room) { AppDialog.ShowInformation(this, Text, "اختر فصلًا."); return; }
        var start = DateOnly.FromDateTime(_from.Value); var end = DateOnly.FromDateTime(_to.Value);
        if (start <= DateOnly.FromDateTime(DateTime.Today) || end < start || end.DayNumber - start.DayNumber > 366) { AppDialog.ShowInformation(this, Text, "اختر فترة مستقبلية صحيحة لا تتجاوز سنة."); return; }
        IReadOnlyList<AttendanceChildOptionDto> children;
        await using (var s = _scopes.CreateAsyncScope())
        {
            var service = s.ServiceProvider.GetRequiredService<IChildAttendanceService>();
            var result = await service.GetChildrenForAttendanceSheetAsync(room.Id, start, end);
            if (result.IsFailure) { AppDialog.ShowInformation(this, Text, result.Error.Description); return; }
            children = result.Value;
        }
        var dates = Enumerable.Range(0, end.DayNumber - start.DayNumber + 1).Select(start.AddDays).ToArray();
        var headers = new[] { "اسم الطالب" }.Concat(dates.Select(x => x.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).ToArray();
        var rows = children.OrderBy(x => x.FullName).Select(x => (IReadOnlyList<string?>)new[] { x.FullName }.Concat(dates.Select(_ => string.Empty)).ToArray()).ToArray();
        using var dialog = new SaveFileDialog { Title = "حفظ كشف الحضور", Filter = "ملف Excel (*.xlsx)|*.xlsx", DefaultExt = "xlsx", AddExtension = true, FileName = $"كشف_حضور_{room.Name}_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var resultExport = await _excel.ExportAsync(new ExcelExportData("كشف الحضور", headers, rows, $"كشف حضور {room.Name}", $"من {start:dd/MM/yyyy} إلى {end:dd/MM/yyyy}"), dialog.FileName);
        ResultPresenter.Present(this, Text, resultExport);
    }
}
