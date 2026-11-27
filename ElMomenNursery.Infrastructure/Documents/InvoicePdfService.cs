using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Drawing;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Serilog;
using System.Diagnostics;

namespace ElMomenNursery.Infrastructure.Documents;

public sealed class InvoicePdfService(string dataDirectory) : IInvoicePdfService
{
    static InvoicePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        FontManager.RegisterFontFromEmbeddedResource(typeof(InvoicePdfService).Assembly, "ElMomenNursery.Infrastructure.Resources.Fonts.NotoNaskhArabic.ttf");
    }

    public Task<Result<string>> CreateInvoiceFileAsync(InvoiceReceiptDto invoice, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var folder = Path.Combine(dataDirectory, "Invoices", invoice.CreatedAt.LocalDateTime.Year.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var path = Path.Combine(folder, $"{invoice.InvoiceNumber}.pdf");
            var timer = Stopwatch.StartNew();
            try
            {
                Directory.CreateDirectory(folder);
                Document.Create(document => document.Page(page =>
                {
                    page.Size(PageSizes.A5); page.Margin(24); page.DefaultTextStyle(style => style.FontFamily("Noto Naskh Arabic").FontSize(11));
                    page.Content().ContentFromRightToLeft().Column(column =>
                    {
                        column.Item().AlignCenter().Text("حضانة المؤمن").FontSize(20).Bold();
                        column.Item().AlignCenter().Text("فاتورة بيع").FontSize(15).Bold();
                        column.Item().PaddingVertical(8).Text($"رقم الفاتورة: {invoice.InvoiceNumber}    التاريخ: {invoice.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}");
                        if (!string.IsNullOrWhiteSpace(invoice.ChildName ?? invoice.CustomerName)) column.Item().Text($"العميل: {invoice.ChildName ?? invoice.CustomerName}");
                        column.Item().PaddingTop(12).Table(table =>
                        {
                            table.ColumnsDefinition(columns => { columns.RelativeColumn(4); columns.RelativeColumn(1); columns.RelativeColumn(2); columns.RelativeColumn(2); });
                            table.Header(header => { header.Cell().Element(Cell).Text("البيان").Bold(); header.Cell().Element(Cell).Text("الكمية").Bold(); header.Cell().Element(Cell).Text("السعر").Bold(); header.Cell().Element(Cell).Text("الإجمالي").Bold(); });
                            foreach (var line in invoice.Lines) { table.Cell().Element(Cell).Text(line.Description); table.Cell().Element(Cell).Text(line.Quantity.ToString()); table.Cell().Element(Cell).Text(line.UnitPrice.ToString("N2")); table.Cell().Element(Cell).Text(line.Total.ToString("N2")); }
                            static QuestPDF.Infrastructure.IContainer Cell(QuestPDF.Infrastructure.IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(3);
                        });
                        column.Item().PaddingTop(12).AlignLeft().Text($"الإجمالي: {invoice.TotalAmount:N2} ج.م").FontSize(16).Bold();
                        column.Item().Text($"طريقة الدفع: {invoice.PaymentMethod switch { Domain.Enums.PaymentMethod.Cash => "نقدًا", Domain.Enums.PaymentMethod.BankTransfer => "تحويل بنكي", _ => "أخرى" }}");
                        if (!string.IsNullOrWhiteSpace(invoice.Reference)) column.Item().Text($"المرجع: {invoice.Reference}");
                    });
                    if (!string.IsNullOrWhiteSpace(invoice.IssuedByName))
                        page.Footer().AlignLeft().Text($"مُصدر الفاتورة: {invoice.IssuedByName}").FontSize(9);
                })).GeneratePdf(path);
                Log.ForContext<InvoicePdfService>().Information("Invoice PDF generated; operation={Operation}; durationMs={DurationMs}; targetFolder={TargetFolder}", "CreateInvoiceFile", timer.ElapsedMilliseconds, folder);
                return Result.Success<string>(path);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                var kind = (ex is IOException io && (io.HResult & 0xFFFF) is 32 or 33) ? "in-use"
                    : ex is UnauthorizedAccessException or IOException or ArgumentException or NotSupportedException ? "not-writable" : "unknown";
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                Log.ForContext<InvoicePdfService>().Warning(ex, "Invoice PDF generation failed; operation={Operation}; failureKind={FailureKind}; exceptionType={ExceptionType}; exceptionMessage={ExceptionMessage}; durationMs={DurationMs}; targetFolder={TargetFolder}", "CreateInvoiceFile", kind, ex.GetType().FullName, ex.Message, timer.ElapsedMilliseconds, folder);
                return Result.Failure<string>(new Error("Invoices.PdfFailed", "تم حفظ الفاتورة، لكن تعذر إنشاء ملف PDF."));
            }
        }, cancellationToken);
    }
}
