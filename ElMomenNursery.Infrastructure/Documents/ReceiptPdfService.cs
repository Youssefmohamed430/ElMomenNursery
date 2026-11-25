using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Drawing;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Serilog;
using System.Diagnostics;

namespace ElMomenNursery.Infrastructure.Documents;

public sealed class ReceiptPdfService(string dataDirectory) : IReceiptPdfService
{
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
        var timer = Stopwatch.StartNew();
        try
        {
        Directory.CreateDirectory(folder);
        var safeNumber = string.Concat(receipt.ReceiptNumber.Where(c => char.IsLetterOrDigit(c) || c == '-'));
        var suffix = isCopy ? "-نسخة" : string.Empty;
        var path = Path.Combine(folder, $"{safeNumber}{suffix}.pdf");
        var logo = Path.Combine(AppContext.BaseDirectory, "Resources", "Branding", "AlMoumenNurseryLogo.jpeg");
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A5);
            page.Margin(24);
            page.DefaultTextStyle(x => x.FontFamily("Noto Naskh Arabic").FontSize(11).FontColor(Colors.Black));
            page.Content().ContentFromRightToLeft().Column(column =>
            {
                column.Spacing(8);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(c => { c.Item().Text(receipt.NurseryName).Bold().FontSize(17); c.Item().Text("إيصال سداد اشتراك").Bold().FontSize(14);
                        if (!string.IsNullOrWhiteSpace(receipt.NurseryAddress)) c.Item().Text(receipt.NurseryAddress);
                        if (!string.IsNullOrWhiteSpace(receipt.NurseryPhone)) c.Item().Text(receipt.NurseryPhone); });
                    if (File.Exists(logo)) row.ConstantItem(65).Height(54).Image(logo).FitArea();
                });
                if (isCopy) column.Item().AlignCenter().Text("نسخة").Bold().FontSize(13);
                Pair(column, "رقم الإيصال", receipt.ReceiptNumber);
                Pair(column, "التاريخ والوقت", receipt.PaidAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                Pair(column, "الطفل", receipt.ChildName);
                Pair(column, "فترة الاشتراك", $"{receipt.PeriodStart:dd/MM/yyyy} — {receipt.PeriodEnd:dd/MM/yyyy}");
                column.Item().PaddingVertical(3).LineHorizontal(1);
                Pair(column, "إجمالي المستحق", Money(receipt.AmountDue));
                Pair(column, "المدفوع سابقًا", Money(receipt.PaidBefore));
                Pair(column, "المدفوع الآن", Money(receipt.PaidNow));
                Pair(column, "المتبقي", Money(receipt.RemainingAmount));
                Pair(column, "طريقة الدفع", Method(receipt.PaymentMethod));
                if (!string.IsNullOrWhiteSpace(receipt.Reference)) Pair(column, "المرجع", receipt.Reference);
                Pair(column, "المستلم", receipt.ReceivedBy ?? "—");
                if (!string.IsNullOrWhiteSpace(receipt.Notes)) Pair(column, "ملاحظات", receipt.Notes);
                column.Item().PaddingTop(20).Text("توقيع المستلم: ____________________");
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
            Log.ForContext<ReceiptPdfService>().Warning("Receipt PDF generation failed; operation={Operation}; failureKind={FailureKind}; durationMs={DurationMs}; targetFolder={TargetFolder}", "CreateReceiptFile", kind, timer.ElapsedMilliseconds, folder);
            return Result.Failure<string>(exception is IOException && kind == "in-use" ? FileErrors.FileInUse
                : kind == "not-writable" ? FileErrors.NotWritable
                : new Error("Receipts.PdfFailed", "تعذر إنشاء ملف الإيصال."));
        }
        }, cancellationToken);
    }

    private static void Pair(ColumnDescriptor column, string label, string value) =>
        column.Item().Row(row => { row.RelativeItem().Text(value); row.ConstantItem(100).Text(label).Bold(); });
    private static string Money(decimal value) => value.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("ar-EG")) + " ج.م";
    private static string Method(PaymentMethod method) => method switch { PaymentMethod.Cash => "نقدًا", PaymentMethod.BankTransfer => "تحويل بنكي", _ => "أخرى" };
}
