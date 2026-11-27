using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Infrastructure.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Drawing;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Serilog;
using System.Diagnostics;

namespace ElMomenNursery.Infrastructure.Documents;

public sealed class ReceiptPdfService(string dataDirectory, PrinterOptions? printerOptions = null) : IReceiptPdfService
{
    private readonly PrinterOptions _options = printerOptions ?? new PrinterOptions();

    static ReceiptPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        FontManager.RegisterFontFromEmbeddedResource(typeof(ReceiptPdfService).Assembly, "ElMomenNursery.Infrastructure.Resources.Fonts.NotoNaskhArabic.ttf");
    }

    public Task<Result<string>> CreateReceiptFileAsync(PaymentReceiptDto receipt, bool isCopy, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
        cancellationToken.ThrowIfCancellationRequested();
        var year = receipt.PaidAt.LocalDateTime.Year;
        var folder = Path.Combine(dataDirectory, "Receipts", year.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var safeNumber = string.Concat(receipt.ReceiptNumber.Where(c => char.IsLetterOrDigit(c) || c == '-'));
        var suffix = isCopy ? "-نسخة" : string.Empty;
        var path = Path.Combine(folder, $"{safeNumber}{suffix}.pdf");
        var timer = Stopwatch.StartNew();
        try
        {
        Directory.CreateDirectory(folder);
        var logo = Path.Combine(AppContext.BaseDirectory, "Resources", "Branding", "AlMoumenNurseryLogoTransparent.png");
        if (!File.Exists(logo)) logo = Path.Combine(AppContext.BaseDirectory, "Resources", "Branding", "AlMoumenNurseryLogo.jpeg");

        // Thermal receipt: width from printer options, continuous height role
        var widthMm = _options.PaperWidthMm > 0 ? _options.PaperWidthMm : 80;
        var fontSize = _options.FontSize > 0 ? _options.FontSize : 10f;

        Document.Create(document => document.Page(page =>
        {
            page.ContinuousSize(widthMm, Unit.Millimetre);
            page.MarginHorizontal(4);
            page.MarginVertical(3);
            page.DefaultTextStyle(x => x.FontFamily("Noto Naskh Arabic").FontSize(fontSize).FontColor(Colors.Black));
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
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().AlignCenter().Text(receipt.NurseryName).Bold().FontSize(fontSize + 3);
                        c.Item().AlignCenter().Text("إيصال سداد اشتراك").Bold().FontSize(fontSize + 1);
                        if (!string.IsNullOrWhiteSpace(receipt.NurseryAddress)) c.Item().AlignCenter().Text(receipt.NurseryAddress);
                        if (!string.IsNullOrWhiteSpace(receipt.NurseryPhone)) c.Item().AlignCenter().Text(receipt.NurseryPhone);
                    });
                });
                if (isCopy) column.Item().AlignCenter().Text("نسخة").Bold().FontSize(fontSize + 1);
                Pair(column, "رقم الإيصال", receipt.ReceiptNumber);
                Pair(column, "التاريخ والوقت", receipt.PaidAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                Pair(column, "الطفل", receipt.ChildName);
                Pair(column, "فترة الاشتراك", $"{receipt.PeriodStart:dd/MM/yyyy} — {receipt.PeriodEnd:dd/MM/yyyy}");
                column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Black);
                Pair(column, "إجمالي المستحق", Money(receipt.AmountDue));
                Pair(column, "المدفوع سابقًا", Money(receipt.PaidBefore));
                Pair(column, "المدفوع الآن", Money(receipt.PaidNow));
                Pair(column, "المتبقي", Money(receipt.RemainingAmount));
                Pair(column, "طريقة الدفع", Method(receipt.PaymentMethod));
                if (!string.IsNullOrWhiteSpace(receipt.Reference)) Pair(column, "المرجع", receipt.Reference);
                Pair(column, "المستلم", receipt.ReceivedBy ?? "—");
                if (!string.IsNullOrWhiteSpace(receipt.Notes)) Pair(column, "ملاحظات", receipt.Notes);
                column.Item().PaddingTop(10).Text("توقيع المستلم: ____________________");
                column.Item().AlignCenter().PaddingTop(6).Text("شكرًا لتعاملكم معنا").FontSize(fontSize - 1);
            });
        })).GeneratePdf(path);
        Log.ForContext<ReceiptPdfService>().Information("Receipt PDF generated; operation={Operation}; durationMs={DurationMs}; targetFolder={TargetFolder}", "CreateReceiptFile", timer.ElapsedMilliseconds, folder);
        return Result.Success(path, "تم تجهيز ملف الإيصال.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            var kind = exception is IOException io && (io.HResult & 0xFFFF) is 32 or 33 ? "in-use"
                : exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException ? "not-writable" : "unknown";
            try { if (File.Exists(path)) File.Delete(path); } catch { }
            Log.ForContext<ReceiptPdfService>().Warning(exception, "Receipt PDF generation failed; operation={Operation}; failureKind={FailureKind}; exceptionType={ExceptionType}; exceptionMessage={ExceptionMessage}; durationMs={DurationMs}; targetFolder={TargetFolder}", "CreateReceiptFile", kind, exception.GetType().FullName, exception.Message, timer.ElapsedMilliseconds, folder);
            return Result.Failure<string>(exception is IOException && kind == "in-use" ? FileErrors.FileInUse
                : kind == "not-writable" ? FileErrors.NotWritable
                : new Error("Receipts.PdfFailed", "تعذر إنشاء ملف الإيصال."));
        }
        }, cancellationToken);
    }

    private static void Pair(ColumnDescriptor column, string label, string value) =>
        column.Item().Row(row => { row.RelativeItem().Text(value); row.ConstantItem(62).Text(label).Bold(); });
    private static string Money(decimal value) => value.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("ar-EG")) + " ج.م";
    private static string Method(PaymentMethod method) => method switch { PaymentMethod.Cash => "نقدًا", PaymentMethod.BankTransfer => "تحويل بنكي", _ => "أخرى" };
}
