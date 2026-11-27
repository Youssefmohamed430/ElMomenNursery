using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Infrastructure.Configuration;
using Serilog;

namespace ElMomenNursery.Infrastructure.Documents;

[SupportedOSPlatform("windows")]
public sealed class ReceiptPrinterService(PrinterOptions options) : IReceiptPrinterService
{
    private readonly PrinterOptions _options = options;

    public Task<Result> PrintReceiptAsync(PaymentReceiptDto receipt, bool isCopy, CancellationToken cancellationToken = default) =>
        Task.Run(() => PrintReceipt(receipt, isCopy, cancellationToken), cancellationToken);

    private Result PrintReceipt(PaymentReceiptDto receipt, bool isCopy, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(_options.Name))
            return Result.Failure(new Error("Receipts.PrinterNotConfigured", "لم يتم تحديد اسم طابعة في إعدادات البرنامج. تم إنشاء ملف PDF ويمكن طباعته يدويًا."));

        try
        {
            using var document = new PrintDocument();
            document.PrinterSettings.PrinterName = _options.Name.Trim();
            if (!document.PrinterSettings.IsValid)
                return Result.Failure(new Error("Receipts.PrinterNotFound", $"الطابعة «{_options.Name}» غير متاحة. تحقق من تعريفها واسمها في إعدادات البرنامج."));

            var paperWidth = Math.Max(30, _options.PaperWidthMm);
            var paperHeight = Math.Max(100, _options.PaperHeightMm);
            document.DefaultPageSettings.PaperSize = new PaperSize("Receipt",
                (int)Math.Round(paperWidth / 25.4 * 100), (int)Math.Round(paperHeight / 25.4 * 100));
            document.DefaultPageSettings.Margins = new Margins(8, 8, 8, 8);
            document.DocumentName = $"إيصال {receipt.ReceiptNumber}";

            using var font = new Font(string.IsNullOrWhiteSpace(_options.FontName) ? "Arial" : _options.FontName,
                Math.Clamp(_options.FontSize, 7f, 18f), FontStyle.Regular, GraphicsUnit.Point);
            using var boldFont = new Font(font, FontStyle.Bold);
            using var titleFont = new Font(font.FontFamily, font.Size + 2, FontStyle.Bold, GraphicsUnit.Point);
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Branding", "AlMoumenNurseryLogoTransparent.png");
            var lines = BuildLines(receipt, isCopy);

            document.PrintPage += (_, e) =>
            {
                if (e.Graphics is null) { e.HasMorePages = false; return; }
                var graphics = e.Graphics;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using var format = new StringFormat(StringFormatFlags.DirectionRightToLeft)
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.Word
                };

                var bounds = e.MarginBounds;
                var y = (float)bounds.Top;
                var textWidth = (float)bounds.Width;
                if (File.Exists(logoPath))
                {
                    using var logo = Image.FromFile(logoPath);
                    var maxWidth = Math.Min(72f, textWidth * .28f);
                    var ratio = maxWidth / logo.Width;
                    var logoHeight = logo.Height * ratio;
                    graphics.DrawImage(logo, bounds.Left + (textWidth - maxWidth) / 2, y, maxWidth, logoHeight);
                    y += logoHeight + 4;
                }

                foreach (var line in lines)
                {
                    var currentFont = line.IsTitle ? titleFont : line.IsBold ? boldFont : font;
                    var measured = graphics.MeasureString(line.Text, currentFont, new SizeF(textWidth, bounds.Height), format);
                    var lineHeight = Math.Max(currentFont.GetHeight(graphics) + 3, measured.Height + 2);
                    if (y + lineHeight > bounds.Bottom) break;
                    graphics.DrawString(line.Text, currentFont, Brushes.Black,
                        new RectangleF(bounds.Left, y, textWidth, lineHeight), format);
                    y += lineHeight;
                }
                e.HasMorePages = false;
            };

            cancellationToken.ThrowIfCancellationRequested();
            document.Print();
            Log.Information("Receipt printed; receiptNumber={ReceiptNumber}; printer={Printer}; copy={IsCopy}",
                receipt.ReceiptNumber, _options.Name, isCopy);
            return Result.Success("تم إرسال الإيصال إلى الطابعة.");
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Receipt print failed; receiptNumber={ReceiptNumber}; printer={Printer}", receipt.ReceiptNumber, _options.Name);
            return Result.Failure(new Error("Receipts.PrintFailed", "تعذرت طباعة الإيصال. تحقق من الطابعة؛ ملف PDF محفوظ ويمكن طباعته يدويًا."));
        }
    }

    private static IReadOnlyList<ReceiptLine> BuildLines(PaymentReceiptDto receipt, bool isCopy)
    {
        var lines = new List<ReceiptLine>
        {
            new(receipt.NurseryName, true, true),
            new("إيصال سداد اشتراك", true),
        };
        if (!string.IsNullOrWhiteSpace(receipt.NurseryAddress)) lines.Add(new(receipt.NurseryAddress));
        if (!string.IsNullOrWhiteSpace(receipt.NurseryPhone)) lines.Add(new(receipt.NurseryPhone));
        if (isCopy) lines.Add(new("نسخة", true));
        lines.Add(new("────────────────────────"));
        lines.Add(new($"رقم الإيصال: {receipt.ReceiptNumber}"));
        lines.Add(new($"التاريخ: {receipt.PaidAt.ToLocalTime():dd/MM/yyyy HH:mm}"));
        lines.Add(new($"الطفل: {receipt.ChildName}"));
        lines.Add(new($"الفترة: {receipt.PeriodStart:dd/MM/yyyy} — {receipt.PeriodEnd:dd/MM/yyyy}"));
        lines.Add(new("────────────────────────"));
        lines.Add(new($"المستحق: {Money(receipt.AmountDue)}"));
        lines.Add(new($"المدفوع سابقًا: {Money(receipt.PaidBefore)}"));
        lines.Add(new($"المدفوع الآن: {Money(receipt.PaidNow)}", true));
        lines.Add(new($"المتبقي: {Money(receipt.RemainingAmount)}"));
        lines.Add(new($"طريقة الدفع: {Method(receipt.PaymentMethod)}"));
        if (!string.IsNullOrWhiteSpace(receipt.Reference)) lines.Add(new($"المرجع: {receipt.Reference}"));
        lines.Add(new($"المستلم: {receipt.ReceivedBy ?? "—"}"));
        if (!string.IsNullOrWhiteSpace(receipt.Notes)) lines.Add(new($"ملاحظات: {receipt.Notes}"));
        lines.Add(new("توقيع المستلم: ____________________"));
        lines.Add(new("شكرًا لتعاملكم معنا", true));
        return lines;
    }

    private static string Money(decimal value) => value.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("ar-EG")) + " ج.م";
    private static string Method(ElMomenNursery.Domain.Enums.PaymentMethod method) => method switch
    {
        ElMomenNursery.Domain.Enums.PaymentMethod.Cash => "نقدًا",
        ElMomenNursery.Domain.Enums.PaymentMethod.BankTransfer => "تحويل بنكي",
        _ => "أخرى"
    };

    private sealed record ReceiptLine(string Text, bool IsBold = false, bool IsTitle = false);
}
