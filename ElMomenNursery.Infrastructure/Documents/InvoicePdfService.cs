using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Serilog;
using System.Diagnostics;
using System.Globalization;

namespace ElMomenNursery.Infrastructure.Documents;

public sealed class InvoicePdfService(string dataDirectory, IServiceScopeFactory scopeFactory) : IInvoicePdfService
{
    static InvoicePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        FontManager.RegisterFontFromEmbeddedResource(typeof(InvoicePdfService).Assembly, "ElMomenNursery.Infrastructure.Resources.Fonts.NotoNaskhArabic.ttf");
    }

    public async Task<Result<string>> CreateInvoiceFileAsync(InvoiceReceiptDto invoice, CancellationToken cancellationToken = default)
    {
        var fontSize = 11;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var setting = await uow.Repository<AppSetting>().FirstOrDefaultAsync(x => x.Key == "Invoices.FontSize", cancellationToken: cancellationToken);
            if (setting is not null && int.TryParse(setting.Value, out var configured) && configured is >= 8 and <= 18)
                fontSize = configured;
        }

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var folder = Path.Combine(dataDirectory, "Invoices", invoice.CreatedAt.LocalDateTime.Year.ToString(CultureInfo.InvariantCulture));
            var path = Path.Combine(folder, $"{invoice.InvoiceNumber}.pdf");
            var timer = Stopwatch.StartNew();
            try
            {
                Directory.CreateDirectory(folder);
                var logo = Path.Combine(AppContext.BaseDirectory, "Resources", "Branding", "AlMoumenNurseryLogoTransparent.png");
                if (!File.Exists(logo)) logo = Path.Combine(AppContext.BaseDirectory, "Resources", "Branding", "AlMoumenNurseryLogo.jpeg");
                Document.Create(document => document.Page(page =>
                {
                    page.ContinuousSize(80, Unit.Millimetre);
                    page.MarginHorizontal(4);
                    page.MarginVertical(3);
                    page.DefaultTextStyle(style => style.FontFamily("Noto Naskh Arabic").FontSize(fontSize).FontColor(Colors.Black));
                    page.Content().ContentFromRightToLeft().Column(column =>
                    {
                        column.Spacing(4);
                        column.Item().AlignCenter().Row(row =>
                        {
                            if (File.Exists(logo))
                            {
                                row.ConstantItem(46).Height(38).Image(logo).FitArea();
                                row.ConstantItem(4);
                            }
                            row.RelativeItem().Column(header =>
                            {
                                header.Item().AlignCenter().Text(invoice.NurseryName).Bold().FontSize(fontSize + 3);
                                header.Item().AlignCenter().Text("فاتورة بيع").Bold().FontSize(fontSize + 1);
                                if (!string.IsNullOrWhiteSpace(invoice.NurseryAddress)) header.Item().AlignCenter().Text(invoice.NurseryAddress);
                                if (!string.IsNullOrWhiteSpace(invoice.NurseryPhone)) header.Item().AlignCenter().Text(invoice.NurseryPhone);
                            });
                        });

                        column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Black);
                        Pair(column, "رقم الفاتورة", invoice.InvoiceNumber);
                        Pair(column, "التاريخ والوقت", invoice.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("ar-EG")));
                        var customer = invoice.ChildName ?? invoice.CustomerName;
                        if (!string.IsNullOrWhiteSpace(customer)) Pair(column, invoice.ChildName is null ? "العميل" : "الطفل", customer);

                        column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Black);
                        foreach (var line in invoice.Lines)
                        {
                            column.Item().Column(item =>
                            {
                                item.Item().Row(row =>
                                {
                                    row.RelativeItem().Text(line.Description).Bold();
                                    row.ConstantItem(78).AlignLeft().Text(Money(line.Total)).Bold();
                                });
                                item.Item().Text($"الكمية: {line.Quantity} × {Money(line.UnitPrice)}").FontSize(Math.Max(8, fontSize - 1));
                                item.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            });
                        }

                        column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Black);
                        Pair(column, "إجمالي الفاتورة", Money(invoice.TotalAmount), bold: true);
                        Pair(column, "المدفوع سابقًا", Money(0));
                        Pair(column, "المدفوع الآن", Money(invoice.TotalAmount), bold: true);
                        Pair(column, "المتبقي", Money(0));
                        Pair(column, "طريقة الدفع", Method(invoice.PaymentMethod));
                        if (!string.IsNullOrWhiteSpace(invoice.Reference)) Pair(column, "المرجع", invoice.Reference);
                        Pair(column, "مُصدر الفاتورة", invoice.IssuedByName ?? "—");
                        if (!string.IsNullOrWhiteSpace(invoice.Notes)) Pair(column, "ملاحظات", invoice.Notes);
                        column.Item().PaddingTop(8).Text("توقيع المستلم: ____________________");
                        column.Item().AlignCenter().PaddingTop(5).Text("شكرًا لتعاملكم معنا").FontSize(Math.Max(8, fontSize - 1));
                    });
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

    private static void Pair(ColumnDescriptor column, string label, string value, bool bold = false) =>
        column.Item().Row(row =>
        {
            var valueText = row.RelativeItem().Text(value);
            if (bold) valueText.Bold();
            row.ConstantItem(64).AlignRight().Text(label).Bold();
        });

    private static string Money(decimal value) => value.ToString("N2", CultureInfo.GetCultureInfo("ar-EG")) + " ج.م";

    private static string Method(Domain.Enums.PaymentMethod method) => method switch
    {
        Domain.Enums.PaymentMethod.Cash => "نقدًا",
        Domain.Enums.PaymentMethod.BankTransfer => "تحويل بنكي",
        _ => "أخرى"
    };
}
