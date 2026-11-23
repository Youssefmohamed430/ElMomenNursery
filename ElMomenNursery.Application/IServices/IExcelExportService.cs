using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public sealed record ExcelExportData(
    string WorksheetName,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string?>> Rows,
    string? Title = null,
    string? FilterDescription = null);

public interface IExcelExportService
{
    Task<Result> ExportAsync(ExcelExportData data, string filePath, CancellationToken cancellationToken = default);
}
