using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using System.Globalization;

namespace ElMomenNursery.Forms;

internal static class GridExcelExport
{
    public static async Task SaveAsync(IWin32Window owner, DataGridView grid, IExcelExportService exporter,
        string title, string worksheetName)
    {
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
            Title = title,
            Filter = "ملف Excel (*.xlsx)|*.xlsx",
            DefaultExt = "xlsx",
            AddExtension = true,
            FileName = $"{worksheetName}_{DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture)}.xlsx",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(owner) != DialogResult.OK) return;

        try
        {
            var data = new ExcelExportData(worksheetName,
                visibleColumns.Select(column => column.HeaderText).ToArray(), rows);
            var result = await exporter.ExportAsync(data, dialog.FileName);
            ResultPresenter.Present(owner, title, result);
        }
        catch (Exception)
        {
            MessageBox.Show(owner, "تعذر إنشاء ملف Excel. تأكد من إمكانية الكتابة إلى المجلد ثم أعد المحاولة.",
                title, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
        }
    }
}
