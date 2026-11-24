using ClosedXML.Excel;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using Serilog;
using System.Diagnostics;

namespace ElMomenNursery.Infrastructure.Spreadsheet;

public sealed class ExcelExportService : IExcelExportService
{
    public async Task<Result> ExportAsync(ExcelExportData data, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (data.Headers.Count == 0) return CommonErrors.InvalidRequest;

        var targetFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
        var timer = Stopwatch.StartNew();
        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(SanitizeWorksheetName(data.WorksheetName));
                worksheet.RightToLeft = true;

            var hasTitle = !string.IsNullOrWhiteSpace(data.Title);
            var hasFilters = !string.IsNullOrWhiteSpace(data.FilterDescription);
            var headerRow = 1;
            if (hasTitle) { worksheet.Cell(1, 1).Value = data.Title; worksheet.Cell(1, 1).Style.Font.Bold = true; worksheet.Cell(1, 1).Style.Font.FontSize = 16; worksheet.Range(1, 1, 1, data.Headers.Count).Merge(); headerRow++; }
            if (hasFilters) { worksheet.Cell(headerRow, 1).Value = $"الفلاتر المستخدمة: {data.FilterDescription}"; worksheet.Range(headerRow, 1, headerRow, data.Headers.Count).Merge(); worksheet.Cell(headerRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; headerRow++; }
            for (var column = 0; column < data.Headers.Count; column++)
            {
                var header = worksheet.Cell(headerRow, column + 1);
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
                    worksheet.Cell(row + headerRow + 1, column + 1).Value = value ?? string.Empty;
                }
            }

            worksheet.SheetView.FreezeRows(headerRow);
            if (data.Rows.Count > 0)
                worksheet.Range(headerRow, 1, data.Rows.Count + headerRow, data.Headers.Count).SetAutoFilter();
            worksheet.Columns(1, data.Headers.Count).AdjustToContents(10, 50);
                workbook.SaveAs(filePath);
            }, cancellationToken);
            Log.ForContext<ExcelExportService>().Information("Excel export completed; operation={Operation}; durationMs={DurationMs}; targetFolder={TargetFolder}",
                "ExportExcel", timer.ElapsedMilliseconds, targetFolder);
            return Result.Success("تم حفظ ملف Excel بنجاح.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            var kind = exception is IOException io && (io.HResult & 0xFFFF) is 32 or 33 ? "in-use"
                : exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException ? "not-writable" : "unknown";
            Log.ForContext<ExcelExportService>().Warning("Excel export failed; operation={Operation}; failureKind={FailureKind}; durationMs={DurationMs}; targetFolder={TargetFolder}",
                "ExportExcel", kind, timer.ElapsedMilliseconds, targetFolder);
            return exception is IOException && kind == "in-use" ? FileErrors.FileInUse
                : kind == "not-writable" ? FileErrors.NotWritable
                : CommonErrors.InvalidRequest;
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
