using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

internal sealed class SettingsPage : UserControl
{
    private readonly IServiceScopeFactory _scopes;
    private readonly NumericUpDown _invoiceFont = new() { Minimum = 8, Maximum = 18, Value = 11, Width = 180 };
    private readonly Button _save = MainForm.CreateActionButton("حفظ إعدادات الفاتورة", true);
    private GroupBox _invoiceCard = null!;
    private GroupBox _lookupCard = null!;
    private GroupBox _backupCard = null!;

    public SettingsPage(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 248, 246);
        RightToLeft = RightToLeft.Yes;
        Padding = new Padding(24);
        Font = new Font("Segoe UI", 10F);
        var title = new Label { Text = "إعدادات النظام", Font = new Font("Segoe UI", 21, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(35, 75, 54), Location = new Point(0, 0) };
        var invoice = _invoiceCard = CreateCard("إعدادات الفواتير", 170);
        var fontLabel = new Label { Text = "حجم الخط في الفاتورة", AutoSize = true, Location = new Point(0, 38), Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
        _invoiceFont.Location = new Point(0, 68); _invoiceFont.Height = 34; _invoiceFont.Font = new Font("Segoe UI", 11F);
        var hint = new Label { Text = "اختر حجم الخط بالنقاط. سيُطبق على الفواتير الجديدة.", AutoSize = false, Height = 28, ForeColor = Color.FromArgb(85, 96, 89), Location = new Point(0, 108), TextAlign = ContentAlignment.MiddleRight };
        _save.Location = new Point(0, 48); _save.Width = 210; _save.Height = 42; _save.Click += async (_, _) => await SaveInvoiceSettingsAsync();
        invoice.Controls.AddRange([fontLabel, _invoiceFont, hint, _save]);

        var lookups = _lookupCard = CreateCard("إدارة القوائم", 140);
        var lookupInfo = new Label { Text = "إدارة طرق الدفع والمسميات الوظيفية ومصادر الإيراد ومواعيد الدوام.", AutoSize = false, Height = 42, Location = new Point(0, 38), TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.FromArgb(55, 68, 60) };
        var lookupButton = MainForm.CreateActionButton("فتح إدارة القوائم"); lookupButton.Location = new Point(0, 82); lookupButton.Width = 190; lookupButton.Height = 42;
        lookupButton.Click += (_, _) => { using var form = new LookupsManagementForm(_scopes); form.ShowDialog(FindForm()); };
        lookups.Controls.AddRange([lookupInfo, lookupButton]);

        var backup = _backupCard = CreateCard("النسخ الاحتياطي", 140);
        var backupInfo = new Label { Text = "احفظ نسخة من قاعدة البيانات لاستعادتها عند الحاجة.", AutoSize = false, Height = 42, Location = new Point(0, 38), TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.FromArgb(55, 68, 60) };
        var backupButton = MainForm.CreateActionButton("إنشاء نسخة احتياطية", true); backupButton.Location = new Point(0, 82); backupButton.Width = 210; backupButton.Height = 42;
        backupButton.Click += async (_, _) => await CreateBackupAsync(backupButton);
        backup.Controls.AddRange([backupInfo, backupButton]);
        Controls.AddRange([invoice, lookups, backup, title]);
        Resize += (_, _) => LayoutCards();
        LayoutCards();
        Load += async (_, _) => await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        await using var scope = _scopes.CreateAsyncScope();
        var setting = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Repository<AppSetting>()
            .FirstOrDefaultAsync(x => x.Key == "Invoices.FontSize");
        if (setting is not null && int.TryParse(setting.Value, out var value) && value is >= 8 and <= 18) _invoiceFont.Value = value;
    }

    private async Task SaveInvoiceSettingsAsync()
    {
        _save.Enabled = false;
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var setting = await uow.Repository<AppSetting>().FirstOrDefaultAsync(x => x.Key == "Invoices.FontSize", asNoTracking: false);
            if (setting is null) await uow.Repository<AppSetting>().AddAsync(new AppSetting { Key = "Invoices.FontSize", Value = ((int)_invoiceFont.Value).ToString(), Description = "حجم خط الفواتير الجديدة." });
            else setting.Value = ((int)_invoiceFont.Value).ToString();
            await uow.SaveChangesAsync();
            AppDialog.ShowInformation(OwnerWindow(), "إعدادات الفواتير", "تم حفظ حجم الخط.");
        }
        catch { AppDialog.ShowInformation(OwnerWindow(), "إعدادات الفواتير", "تعذر حفظ الإعداد. أعد المحاولة."); }
        finally { if (!IsDisposed) _save.Enabled = true; }
    }

    private async Task CreateBackupAsync(Button button)
    {
        using var picker = new SaveFileDialog { Title = "حفظ نسخة احتياطية من قاعدة البيانات", Filter = "نسخة قاعدة بيانات الحضانة (*.db)|*.db", FileName = $"AlMoumenNursery-backup-{DateTime.Now:yyyyMMdd-HHmm}.db", DefaultExt = "db", AddExtension = true, OverwritePrompt = true };
        if (picker.ShowDialog(FindForm()) != DialogResult.OK) return;
        button.Enabled = false;
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>().CreateBackupAsync(picker.FileName);
            AppDialog.ShowInformation(OwnerWindow(), "نسخ احتياطي", result.IsSuccess ? $"تم إنشاء النسخة بنجاح.\n{picker.FileName}" : result.Error.Description);
        }
        catch { AppDialog.ShowInformation(OwnerWindow(), "نسخ احتياطي", "تعذر إنشاء النسخة. تحقق من صلاحية الكتابة في المكان المحدد."); }
        finally { if (!IsDisposed) button.Enabled = true; }
    }

    private IWin32Window OwnerWindow() => FindForm() is { } form ? form : this;

    private static GroupBox CreateCard(string title, int height) => new()
    {
        Text = title, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Height = height,
        BackColor = Color.White, ForeColor = Color.FromArgb(35, 75, 54), Padding = new Padding(18),
        Anchor = AnchorStyles.Top | AnchorStyles.Right
    };

    private void LayoutCards()
    {
        var width = Math.Clamp(ClientSize.Width - Padding.Horizontal, 480, 860);
        var right = ClientSize.Width - Padding.Right;
        _invoiceCard.SetBounds(right - width, 58, width, 170);
        _lookupCard.SetBounds(right - width, 244, width, 140);
        _backupCard.SetBounds(right - width, 400, width, 140);
        var buttonLeft = width - 230;
        _invoiceFont.Left = width - 230;
        _invoiceFont.Width = 200;
        _invoiceCard.Controls[0].Left = width - 420;
        _invoiceCard.Controls[0].Width = 390;
        _invoiceCard.Controls[2].Left = 20;
        _invoiceCard.Controls[2].Width = width - 260;
        _save.Left = 20;
        _lookupCard.Controls[0].Left = 20; _lookupCard.Controls[0].Width = width - 50;
        _lookupCard.Controls[1].Left = buttonLeft;
        _backupCard.Controls[0].Left = 20; _backupCard.Controls[0].Width = width - 50;
        _backupCard.Controls[1].Left = buttonLeft;
    }
}
