namespace ElMomenNursery.Application.IServices;

public sealed record ExcelExportData(
    string WorksheetName,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string?>> Rows);

public interface IExcelExportService
{
    Task ExportAsync(ExcelExportData data, string filePath, CancellationToken cancellationToken = default);
}
