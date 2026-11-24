using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using System.Globalization;

namespace ElMomenNursery.Forms;

internal static class GridExcelExport
{
    public static async Task SaveAsync(IWin32Window owner, DataGridView grid, IExcelExportService exporter,
        string title, string worksheetName)
    {
        var filterDescription = GetFilterDescription(grid);
        var reportTitle = $"{title} - {filterDescription}";
        var visibleColumns = grid.Columns.Cast<DataGridViewColumn>()
            .Where(column => column.Visible)
            .OrderBy(column => column.DisplayIndex)
            .ToArray();
        var rows = grid.Rows.Cast<DataGridViewRow>()
            .Where(row => row.Visible && !row.IsNewRow)
            .Select(row => (IReadOnlyList<string?>)visibleColumns
                .Select(column => Convert.ToString(row.Cells[column.Index].FormattedValue, CultureInfo.CurrentCulture))
                .ToArray())
            .ToArray();

        if (visibleColumns.Length == 0)
        {
            MessageBox.Show(owner, "لا توجد أعمدة ظاهرة لتصديرها.", title, MessageBoxButtons.OK,
                MessageBoxIcon.Information, MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = reportTitle,
            Filter = "ملف Excel (*.xlsx)|*.xlsx",
            DefaultExt = "xlsx",
            AddExtension = true,
            FileName = $"{SafeFilePart(reportTitle)}_{DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture)}.xlsx",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(owner) != DialogResult.OK) return;

        var targetFolder = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var data = new ExcelExportData(worksheetName,
                visibleColumns.Select(column => column.HeaderText).ToArray(), rows, reportTitle, filterDescription);
            var result = await exporter.ExportAsync(data, dialog.FileName);
            ResultPresenter.Present(owner, title, result);
        }
        catch (Exception ex)
        {
            var kind = ex is IOException io && (io.HResult & 0xFFFF) is 32 or 33 ? "in-use"
                : ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException ? "not-writable" : "unknown";
            Serilog.Log.ForContext(typeof(GridExcelExport)).Warning("Grid Excel export failed; operation={Operation}; failureKind={FailureKind}; durationMs={DurationMs}; targetFolder={TargetFolder}",
                "SaveAsync", kind, timer.ElapsedMilliseconds, targetFolder);
            MessageBox.Show(owner, "تعذر إنشاء ملف Excel. تأكد من إمكانية الكتابة إلى المجلد ثم أعد المحاولة.",
                title, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
        }
    }
    private static string GetFilterDescription(DataGridView grid)
    {
        FilterCard? card = null;
        for (Control? current = grid.Parent; current is not null && card is null; current = current.Parent)
            card = FindFilterCard(current);
        if (card is null) return "كل البيانات";

        var values = new List<string>();
        foreach (var field in card.Fields)
        {
            var value = DescribeControl(field.Input);
            if (value is not null) values.Add($"{field.Label.Text}: {value}");
        }
        foreach (var group in card.Controls.OfType<FilterGroup>())
        {
            var groupValues = group.Controls.Cast<Control>().Where(control => control is not Label)
                .Select(DescribeControl).Where(value => value is not null).ToArray();
            if (groupValues.Length > 0) values.Add($"{group.Label.Text}: {string.Join(" إلى ", groupValues)}");
        }
        return values.Count == 0 ? "كل البيانات" : string.Join("؛ ", values);
    }

    private static FilterCard? FindFilterCard(Control root)
    {
        if (root is FilterCard filter) return filter;
        foreach (Control child in root.Controls)
            if (FindFilterCard(child) is { } found) return found;
        return null;
    }

    private static string? DescribeControl(Control control)
    {
        if (control is FilterSearchBox search) return DescribeText(search.Text);
        if (control is ComboBox combo)
        {
            var value = combo.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(value) || value == "الكل" || value.StartsWith("كل ", StringComparison.Ordinal)) return null;
            return value;
        }
        if (control is DateTimePicker date)
        {
            if (date.ShowCheckBox && !date.Checked) return null;
            return date.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
        if (control is TextBox text) return DescribeText(text.Text);
        if (control is NumericUpDown number) return number.Value == 0 ? null : number.Value.ToString(CultureInfo.CurrentCulture);
        if (control is CheckBox check) return check.Checked ? "نعم" : null;
        return null;
    }

    private static string? DescribeText(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SafeFilePart(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, ' ');
        value = string.Join("_", value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return value.Length > 100 ? value[..100] : value;
    }

}
