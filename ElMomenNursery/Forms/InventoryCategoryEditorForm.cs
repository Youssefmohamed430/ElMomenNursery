using System.Drawing;
using Serilog;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

public sealed class InventoryCategoryEditorForm : Form
{
    private static readonly ILogger Log = Serilog.Log.ForContext<InventoryCategoryEditorForm>();
    private readonly IServiceScopeFactory? _scopes;
    private readonly InventoryCategoryDto? _category;
    private readonly TextBox _name = new() { Width = 380, MaxLength = 100 };
    private readonly TextBox _description = new() { Width = 380, Height = 120, Multiline = true, MaxLength = 500, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox _active = new() { Text = "الفئة نشطة", Checked = true, AutoSize = true };
    private readonly Button _save = MainForm.CreateActionButton("حفظ", true);

    public InventoryCategoryEditorForm() : this(null, null) { _name.Text = "فئة تجريبية"; }
    public InventoryCategoryEditorForm(IServiceScopeFactory? scopes, InventoryCategoryDto? category = null)
    {
        _scopes = scopes; _category = category; Text = category is null ? "إضافة فئة" : "تعديل فئة";
        StartPosition = FormStartPosition.CenterParent; MinimumSize = new Size(560, 450); Size = new Size(670, 520);
        RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; Font = new Font("Segoe UI", 10F); BackColor = BrandPalette.AppBackground;
        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), ColumnCount = 2, RowCount = 4, RightToLeft = RightToLeft.Yes, BackColor = BrandPalette.Surface };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(fields, "اسم الفئة *", _name, 0, 58); AddField(fields, "الوصف", _description, 1, 150);
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); fields.Controls.Add(new Label { Text = "الحالة", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 2); fields.Controls.Add(_active, 1, 2);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 66, FlowDirection = FlowDirection.LeftToRight, RightToLeft = RightToLeft.Yes, Padding = new Padding(16) };
        _save.Width = 125; var cancel = MainForm.CreateActionButton("إلغاء"); cancel.Width = 110; cancel.DialogResult = DialogResult.Cancel;
        _save.Click += async (_, _) => await SaveAsync(); footer.Controls.AddRange([_save, cancel]); Controls.Add(fields); Controls.Add(footer);
        AcceptButton = _save; CancelButton = cancel;
        if (category is not null) { _name.Text = category.Name; _description.Text = category.Description ?? string.Empty; _active.Checked = category.IsActive; }
    }

    private static void AddField(TableLayoutPanel table, string label, Control control, int row, int height)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        table.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, RightToLeft = RightToLeft.Yes }, 0, row);
        control.Dock = DockStyle.Fill; control.Margin = new Padding(8); table.Controls.Add(control, 1, row);
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_name.Text)) { AppDialog.ShowInformation(this, "الفئة", "أدخل اسم الفئة."); _name.Focus(); return; }
        if (_scopes is null) { DialogResult = DialogResult.OK; Close(); return; }
        _save.Enabled = false;
        try
        {
            await using var scope = _scopes.CreateAsyncScope(); var service = scope.ServiceProvider.GetRequiredService<IInventoryCategoryService>();
            var result = _category is null
                ? await service.CreateCategoryAsync(new CreateCategoryDto { Name = _name.Text.Trim(), Description = EmptyToNull(_description.Text), IsActive = _active.Checked })
                : await service.UpdateCategoryAsync(_category.Id, new UpdateCategoryDto { Name = _name.Text.Trim(), Description = EmptyToNull(_description.Text), IsActive = _active.Checked });
            if (IsDisposed || !ResultPresenter.Present(this, "الفئة", result)) return;
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { Log.Error(ex, "Inventory category save failed"); if (!IsDisposed) AppDialog.ShowInformation(this, "الفئة", "تعذر حفظ الفئة. أعد المحاولة."); }
        finally { if (!IsDisposed) _save.Enabled = true; }
    }
    private static string? EmptyToNull(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
