using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

internal sealed class LookupsManagementForm : ResponsiveForm
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ComboBox _category = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RightToLeft = RightToLeft.Yes, RowHeadersVisible = false };
    public LookupsManagementForm(IServiceScopeFactory scopes)
    {
        _scopes = scopes; Text = "إدارة القوائم"; Width = 760; Height = 520; MinimumSize = new Size(560, 380); RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
        foreach (var pair in new[] { ("PaymentMethod", "طرق الدفع"), ("IncomeSource", "مصادر الإيراد"), ("ExpenseCategory", "فئات المصروف"), ("JobDesignation", "المسميات الوظيفية"), ("EmployeeShiftStartTime", "مواعيد بداية دوام الموظفين"), ("FinanceType", "أنواع الحركة المالية") }) _category.Items.Add(new CategoryOption(pair.Item1, pair.Item2));
        _category.SelectedIndex = 0;
        _grid.Columns.Add("Name", "الاسم"); _grid.Columns.Add("Sort", "الترتيب"); _grid.Columns.Add("Active", "الحالة"); _grid.Columns.Add("System", "أساسي");
        var add = new Button { Text = "إضافة", AutoSize = true }; var edit = new Button { Text = "تعديل", AutoSize = true }; var toggle = new Button { Text = "تفعيل/إيقاف", AutoSize = true }; var delete = new Button { Text = "حذف", AutoSize = true };
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, RightToLeft = RightToLeft.Yes }; toolbar.Controls.AddRange([add, edit, toggle, delete]);
        Controls.Add(_grid); Controls.Add(toolbar); Controls.Add(_category); _category.SelectedIndexChanged += async (_, _) => await LoadRows();
        add.Click += async (_, _) => await Edit(null); edit.Click += async (_, _) => { if (Selected() is { } item) await Edit(item); };
        toggle.Click += async (_, _) => { if (Selected() is { } item) await Save(item, item.Name, item.SortOrder, !item.IsActive); };
        delete.Click += async (_, _) => await UiGuard.RunAsync(this, "Lookups.Delete", async () =>
        {
            if (Selected() is not { } item)
            {
                AppDialog.ShowInformation(this, "إدارة القوائم", "اختر عنصرًا من الجدول أولًا.");
                return;
            }
            if (!AppDialog.Confirm(this, "حذف عنصر", $"هل تريد حذف «{item.Name}» من القائمة؟ لن تتأثر السجلات السابقة التي استخدمت هذه القيمة.")) return;

            await using var scope = _scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<ILookupService>().DeleteAsync(item.Id);
            if (!ResultPresenter.Present(this, "إدارة القوائم", result)) return;
            await LoadRows();
        }, "تعذر حذف العنصر من القائمة.");
        Shown += async (_, _) => await LoadRows();
    }
    private LookupItemDto? Selected() => _grid.SelectedRows.Count == 0 ? null : _grid.SelectedRows[0].Tag as LookupItemDto;
    private async Task LoadRows()
    {
        if (_category.SelectedItem is not CategoryOption option) return;
        await using var scope = _scopes.CreateAsyncScope(); var result = await scope.ServiceProvider.GetRequiredService<ILookupService>().GetAsync(option.Id, true);
        _grid.Rows.Clear(); if (result.IsFailure) { AppDialog.ShowInformation(this, Text, result.Error.Description); return; }
        foreach (var item in result.Value) { var i = _grid.Rows.Add(item.Name, item.SortOrder, item.IsActive ? "نشط" : "موقوف", item.IsSystem ? "نعم" : "لا"); _grid.Rows[i].Tag = item; }
    }
    private async Task Edit(LookupItemDto? item)
    {
        var name = new TextBox { Text = item?.Name ?? "", Width = 240 }; var sort = new NumericUpDown { Minimum = 0, Maximum = 10000, Value = item?.SortOrder ?? _grid.Rows.Count, Width = 240 };
        using var dialog = new ResponsiveForm { Text = item is null ? "إضافة عنصر" : "تعديل عنصر", Width = 370, Height = 220, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
        var ok = new Button { Text = "حفظ", DialogResult = DialogResult.OK, Left = 210, Top = 110 }; dialog.Controls.AddRange([new Label { Text = "الاسم", Left = 25, Top = 20, Width = 90 }, name, new Label { Text = "الترتيب", Left = 25, Top = 65, Width = 90 }, sort, ok]); name.SetBounds(120, 20, 210, 30); sort.SetBounds(120, 65, 210, 30);
        if (dialog.ShowDialog(this) != DialogResult.OK) return; await Save(item, name.Text, (int)sort.Value, item?.IsActive ?? true);
    }
    private async Task Save(LookupItemDto? item, string name, int order, bool active)
    {
        if (_category.SelectedItem is not CategoryOption category) return;
        var dto = new SaveLookupItemDto(category.Id, name, order, active);
        await using var scope = _scopes.CreateAsyncScope(); var service = scope.ServiceProvider.GetRequiredService<ILookupService>();
        var result = item is null ? await service.CreateAsync(dto) : await service.UpdateAsync(item.Id, dto);
        if (result.IsFailure) AppDialog.ShowInformation(this, Text, result.Error.Description); await LoadRows();
    }
    private async Task WithService(Func<ILookupService, Task<ElMomenNursery.Domain.Abstractions.Result>> action) { await using var scope = _scopes.CreateAsyncScope(); var result = await action(scope.ServiceProvider.GetRequiredService<ILookupService>()); if (result.IsFailure) AppDialog.ShowInformation(this, Text, result.Error.Description); }
    private sealed record CategoryOption(string Id, string Label) { public override string ToString() => Label; }
}
