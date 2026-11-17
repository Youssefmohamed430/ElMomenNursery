using ClosedXML.Excel;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Infrastructure.Spreadsheet;

public sealed class ExcelExportService : IExcelExportService
{
    public async Task<Result> ExportAsync(ExcelExportData data, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (data.Headers.Count == 0) return CommonErrors.InvalidRequest;

        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(SanitizeWorksheetName(data.WorksheetName));
                worksheet.RightToLeft = true;

            for (var column = 0; column < data.Headers.Count; column++)
            {
                var header = worksheet.Cell(1, column + 1);
                header.Value = data.Headers[column];
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF2E4");
                header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            for (var row = 0; row < data.Rows.Count; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (var column = 0; column < data.Headers.Count; column++)
                {
                    var value = column < data.Rows[row].Count ? data.Rows[row][column] : null;
                    worksheet.Cell(row + 2, column + 1).Value = value ?? string.Empty;
                }
            }

            worksheet.SheetView.FreezeRows(1);
            if (data.Rows.Count > 0)
                worksheet.Range(1, 1, data.Rows.Count + 1, data.Headers.Count).SetAutoFilter();
            worksheet.Columns(1, data.Headers.Count).AdjustToContents(10, 50);
                workbook.SaveAs(filePath);
            }, cancellationToken);
            return Result.Success("تم حفظ ملف Excel بنجاح.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (IOException exception) when ((exception.HResult & 0xFFFF) is 32 or 33) { return FileErrors.FileInUse; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return FileErrors.NotWritable;
        }
    }

    private static string SanitizeWorksheetName(string name)
    {
        var cleaned = string.IsNullOrWhiteSpace(name) ? "البيانات" : name;
        foreach (var invalid in new[] { ':', '\\', '/', '?', '*', '[', ']' })
            cleaned = cleaned.Replace(invalid, ' ');
        cleaned = cleaned.Trim().Trim('\'');
        if (cleaned.Length > 31) cleaned = cleaned[..31];
        return string.IsNullOrWhiteSpace(cleaned) ? "البيانات" : cleaned;
    }
}
