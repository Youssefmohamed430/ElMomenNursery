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

public sealed class InventoryCategoryEditorForm : ResponsiveForm
{
    private static readonly ILogger Log = Serilog.Log.ForContext<InventoryCategoryEditorForm>();
    private readonly IServiceScopeFactory? _scopes;
    private readonly InventoryCategoryDto? _category;
    private readonly TextBox _name = new() { Width = 380, MaxLength = 100 };
    private readonly TextBox _description = new() { Width = 380, Height = 100, Multiline = true, MaxLength = 500, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox _active = new() { Text = "الفئة نشطة", Checked = true, AutoSize = true };
    private readonly ComboBox _parentCategory = new() { Width = 380, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _save = MainForm.CreateActionButton("حفظ", true);

    public InventoryCategoryEditorForm() : this(null, null) { _name.Text = "فئة تجريبية"; }
    public InventoryCategoryEditorForm(IServiceScopeFactory? scopes, InventoryCategoryDto? category = null)
    {
        _scopes = scopes; _category = category; Text = category is null ? "إضافة فئة" : "تعديل فئة";
        StartPosition = FormStartPosition.CenterParent; MinimumSize = new Size(580, 480); Size = new Size(680, 540);
        RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; Font = new Font("Segoe UI", 10F); BackColor = BrandPalette.AppBackground;

        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), ColumnCount = 2, RowCount = 5, RightToLeft = RightToLeft.Yes, BackColor = BrandPalette.Surface };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddField(fields, "اسم الفئة *", _name, 0, 58);
        AddField(fields, "الفئة الأب", _parentCategory, 1, 58);
        AddField(fields, "الوصف", _description, 2, 110);

        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        fields.Controls.Add(new Label { Text = "الحالة", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 3);
        fields.Controls.Add(_active, 1, 3);

        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 66, FlowDirection = FlowDirection.LeftToRight, RightToLeft = RightToLeft.Yes, Padding = new Padding(16) };
        _save.Width = 125; var cancel = MainForm.CreateActionButton("إلغاء"); cancel.Width = 110; cancel.DialogResult = DialogResult.Cancel;
        _save.Click += async (_, _) => await SaveAsync();
        footer.Controls.AddRange([_save, cancel]);

        Controls.Add(fields);
        Controls.Add(footer);
        AcceptButton = _save; CancelButton = cancel;

        _parentCategory.Items.Add(new CategoryParentChoice(null, "بدون (فئة رئيسية)"));
        _parentCategory.SelectedIndex = 0;

        if (category is not null)
        {
            _name.Text = category.Name;
            _description.Text = category.Description ?? string.Empty;
            _active.Checked = category.IsActive;
        }

        if (_scopes is not null) _ = LoadParentOptionsAsync();
    }

    private static void AddField(TableLayoutPanel table, string label, Control control, int row, int height)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        table.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, RightToLeft = RightToLeft.Yes }, 0, row);
        control.Dock = DockStyle.Fill; control.Margin = new Padding(8); table.Controls.Add(control, 1, row);
    }

    private async Task LoadParentOptionsAsync()
    {
        try
        {
            await using var scope = _scopes!.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IInventoryCategoryService>();
            var result = await service.GetCategoryLookupAsync(false);
            if (result.IsSuccess)
            {
                var excluded = new HashSet<Guid>();
                if (_category is not null)
                {
                    var queue = new Queue<Guid>(); queue.Enqueue(_category.Id);
                    while (queue.Count > 0) { var id = queue.Dequeue(); if (!excluded.Add(id)) continue; foreach (var child in result.Value.Where(x => x.ParentCategoryId == id)) queue.Enqueue(child.Id); }
                }
                var children = result.Value.ToLookup(x => x.ParentCategoryId);
                void AddLevel(Guid? parent, int depth) { foreach (var cat in children[parent].OrderBy(x => x.Name)) { if (excluded.Contains(cat.Id)) continue; _parentCategory.Items.Add(new CategoryParentChoice(cat.Id, new string(' ', depth * 4) + "— " + cat.Name + (cat.IsActive ? "" : " (موقوفة)"))); AddLevel(cat.Id, depth + 1); } }
                _parentCategory.Items.Clear(); _parentCategory.Items.Add(new CategoryParentChoice(null, "بدون (فئة رئيسية)")); AddLevel(null, 0);
                if (_category?.ParentCategoryId is Guid parentId)
                {
                    var match = _parentCategory.Items.Cast<CategoryParentChoice>().FirstOrDefault(x => x.Id == parentId);
                    if (match is not null) _parentCategory.SelectedItem = match;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load parent categories");
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_name.Text)) { AppDialog.ShowInformation(this, "الفئة", "أدخل اسم الفئة."); _name.Focus(); return; }
        if (_scopes is null) { DialogResult = DialogResult.OK; Close(); return; }
        _save.Enabled = false;
        try
        {
            await using var scope = _scopes.CreateAsyncScope(); var service = scope.ServiceProvider.GetRequiredService<IInventoryCategoryService>();
            var parentId = (_parentCategory.SelectedItem as CategoryParentChoice)?.Id;
            var result = _category is null
                ? await service.CreateCategoryAsync(new CreateCategoryDto { Name = _name.Text.Trim(), Description = EmptyToNull(_description.Text), IsActive = _active.Checked, ParentCategoryId = parentId })
                : await service.UpdateCategoryAsync(_category.Id, new UpdateCategoryDto { Name = _name.Text.Trim(), Description = EmptyToNull(_description.Text), IsActive = _active.Checked, ParentCategoryId = parentId });
            if (IsDisposed || !ResultPresenter.Present(this, "الفئة", result)) return;
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { Log.Error(ex, "Inventory category save failed"); if (!IsDisposed) AppDialog.ShowInformation(this, "الفئة", "تعذر حفظ الفئة. أعد المحاولة."); }
        finally { if (!IsDisposed) _save.Enabled = true; }
    }

    private static string? EmptyToNull(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private sealed record CategoryParentChoice(Guid? Id, string Name)
    {
        public override string ToString() => Name;
    }
}
