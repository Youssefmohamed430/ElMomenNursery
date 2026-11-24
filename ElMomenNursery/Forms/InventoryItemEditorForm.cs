using System.Drawing;
using System.Globalization;
using Serilog;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

public sealed class InventoryItemEditorForm : Form
{
    private static readonly ILogger Log = Serilog.Log.ForContext<InventoryItemEditorForm>();
    private readonly IServiceScopeFactory? _scopes;
    private readonly InventoryItemDto? _item;
    private readonly TextBox _name = new() { Width = 360, MaxLength = 100 };
    private readonly ComboBox _category = new() { Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _price = new() { Width = 180, DecimalPlaces = 2, Minimum = 0, Maximum = 9999999999999999m, ThousandsSeparator = true };
    private readonly NumericUpDown _quantity = new() { Width = 180, DecimalPlaces = 0, Minimum = 0, Maximum = int.MaxValue, ThousandsSeparator = true };
    private readonly TextBox _quantityAdjustmentReason = new() { Width = 360, MaxLength = 300 };
    private readonly CheckBox _active = new() { Text = "الصنف نشط", Checked = true, AutoSize = true };
    private readonly Button _save = MainForm.CreateActionButton("حفظ", true);

    public InventoryItemEditorForm() : this(null, null) { _name.Text = "صنف تجريبي"; }
    public InventoryItemEditorForm(IServiceScopeFactory? scopes, InventoryItemDto? item = null)
    {
        _scopes = scopes; _item = item; Text = item is null ? "إضافة صنف" : "تعديل صنف";
        StartPosition = FormStartPosition.CenterParent; MinimumSize = new Size(560, 420); Size = new Size(650, 470);
        RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; Font = new Font("Segoe UI", 10F); BackColor = BrandPalette.AppBackground;
        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), ColumnCount = 2, RowCount = 7, RightToLeft = RightToLeft.Yes, BackColor = BrandPalette.Surface };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(fields, "اسم الصنف *", _name, 0); AddField(fields, "الفئة *", _category, 1); AddField(fields, "سعر الوحدة (ج.م) *", _price, 2); AddField(fields, "الكمية الحالية (قطعة) *", _quantity, 3);
        AddField(fields, "سبب تعديل الكمية", _quantityAdjustmentReason, 4);
        fields.Controls.Add(new Label { Text = "الحالة", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, RightToLeft = RightToLeft.Yes }, 0, 5); fields.Controls.Add(_active, 1, 5);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 66, FlowDirection = FlowDirection.LeftToRight, RightToLeft = RightToLeft.Yes, Padding = new Padding(16) };
        _save.Width = 125; var cancel = MainForm.CreateActionButton("إلغاء"); cancel.Width = 110; cancel.DialogResult = DialogResult.Cancel;
        _save.Click += async (_, _) => await SaveAsync(); footer.Controls.AddRange([_save, cancel]); Controls.Add(fields); Controls.Add(footer);
        AcceptButton = _save; CancelButton = cancel;
        if (item is not null) { _name.Text = item.Name; _price.Value = Math.Clamp(item.UnitPrice, 0m, _price.Maximum); _quantity.Value = Math.Clamp(item.Quantity, 0, int.MaxValue); _active.Checked = item.IsActive; }
        if (_scopes is not null) LoadCategoriesAsync(); else { _category.Items.Add("معاينة الفئة"); _category.SelectedIndex = 0; }
    }

    private static void AddField(TableLayoutPanel table, string label, Control control, int row)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        table.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, RightToLeft = RightToLeft.Yes }, 0, row);
        control.Dock = DockStyle.Fill; control.Margin = new Padding(8, 10, 8, 10); table.Controls.Add(control, 1, row);
    }

    private async void LoadCategoriesAsync()
    {
        try
        {
            Result<IReadOnlyList<InventoryCategoryLookupDto>> result;
            await using (var scope = _scopes!.CreateAsyncScope()) result = await scope.ServiceProvider.GetRequiredService<IInventoryCategoryService>().GetCategoryLookupAsync(true);
            if (IsDisposed) return;
            if (result.IsFailure) { ResultPresenter.Present(this, "الفئات", result); return; }
            foreach (var option in result.Value) _category.Items.Add(option);
            if (_item is not null && !_category.Items.Cast<object>().OfType<InventoryCategoryLookupDto>().Any(x => x.Id == _item.CategoryId))
                _category.Items.Add(new InventoryCategoryLookupDto(_item.CategoryId, _item.CategoryName + " (موقوفة)", false));
            var selected = _category.Items.Cast<object>().OfType<InventoryCategoryLookupDto>().FirstOrDefault(x => x.Id == _item?.CategoryId);
            if (selected is not null) _category.SelectedItem = selected; else if (_category.Items.Count > 0) _category.SelectedIndex = 0;
        }
        catch (Exception ex) { Log.Error(ex, "Inventory item category lookup failed"); if (!IsDisposed) AppDialog.ShowInformation(this, "الأصناف", "تعذر تحميل الفئات."); }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_name.Text)) { AppDialog.ShowInformation(this, "الصنف", "أدخل اسم الصنف."); _name.Focus(); return; }
        if (_category.SelectedItem is not InventoryCategoryLookupDto category) { AppDialog.ShowInformation(this, "الصنف", "اختر فئة للصنف."); _category.Focus(); return; }
        if (_scopes is null) { DialogResult = DialogResult.OK; Close(); return; }
        _save.Enabled = false;
        try
        {
            await using var scope = _scopes.CreateAsyncScope(); var service = scope.ServiceProvider.GetRequiredService<IInventoryItemService>();
            var result = _item is null
                ? await service.CreateItemAsync(new CreateItemDto { Name = _name.Text.Trim(), CategoryId = category.Id, UnitPrice = _price.Value, Quantity = (int)_quantity.Value, IsActive = _active.Checked })
                : await service.UpdateItemAsync(_item.Id, new UpdateItemDto { Name = _name.Text.Trim(), CategoryId = category.Id, UnitPrice = _price.Value, Quantity = (int)_quantity.Value, ExpectedQuantity = _item.Quantity, QuantityAdjustmentReason = _quantityAdjustmentReason.Text, IsActive = _active.Checked });
            if (IsDisposed || !ResultPresenter.Present(this, "الصنف", result)) return;
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { Log.Error(ex, "Inventory item save failed"); if (!IsDisposed) AppDialog.ShowInformation(this, "الصنف", "تعذر حفظ الصنف. أعد المحاولة."); }
        finally { if (!IsDisposed) _save.Enabled = true; }
    }
}
