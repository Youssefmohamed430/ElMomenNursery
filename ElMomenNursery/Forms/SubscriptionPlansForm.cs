using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using Microsoft.Extensions.DependencyInjection;
namespace ElMomenNursery.Forms;
internal sealed class SubscriptionPlansForm : ResponsiveForm
{
    private readonly IServiceScopeFactory _scopes;
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RightToLeft = RightToLeft.Yes, RowHeadersVisible = false };
    public SubscriptionPlansForm(IServiceScopeFactory scopes)
    {
        _scopes = scopes; Text = "خطط الاشتراك"; Width = 720; Height = 480; MinimumSize = new Size(520, 350); RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
        _grid.Columns.Add("Name", "اسم الخطة"); _grid.Columns.Add("Fee", "الرسوم الافتراضية"); _grid.Columns.Add("Description", "الوصف"); _grid.Columns.Add("Active", "الحالة");
        var add = new Button { Text = "إضافة", AutoSize = true }; var edit = new Button { Text = "تعديل", AutoSize = true }; var toggle = new Button { Text = "تفعيل/إيقاف", AutoSize = true }; var delete = new Button { Text = "حذف", AutoSize = true };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, RightToLeft = RightToLeft.Yes }; bar.Controls.AddRange([add, edit, toggle, delete]); Controls.Add(_grid); Controls.Add(bar);
        add.Click += async (_, _) => await Edit(null); edit.Click += async (_, _) => { if (Selected() is { } item) await Edit(item); };
        toggle.Click += async (_, _) => { if (Selected() is { } item) { await using var s = _scopes.CreateAsyncScope(); var r = await s.ServiceProvider.GetRequiredService<ISubscriptionPlanService>().SetActiveAsync(item.Id, !item.IsActive); if (r.IsFailure) AppDialog.ShowInformation(this, Text, r.Error.Description); await LoadRows(); } };
        delete.Click += async (_, _) => { if (Selected() is not { } item) return; await using var s = _scopes.CreateAsyncScope(); var r = await s.ServiceProvider.GetRequiredService<ISubscriptionPlanService>().DeleteAsync(item.Id); if (r.IsFailure) AppDialog.ShowInformation(this, Text, r.Error.Description); await LoadRows(); };
        Shown += async (_, _) => await LoadRows();
    }
    private SubscriptionPlanDto? Selected() => _grid.SelectedRows.Count == 0 ? null : _grid.SelectedRows[0].Tag as SubscriptionPlanDto;
    private async Task LoadRows() { await using var s = _scopes.CreateAsyncScope(); var r = await s.ServiceProvider.GetRequiredService<ISubscriptionPlanService>().GetAsync(); _grid.Rows.Clear(); if (r.IsFailure) { AppDialog.ShowInformation(this, Text, r.Error.Description); return; } foreach (var p in r.Value) { var i = _grid.Rows.Add(p.Name, p.DefaultFee.ToString("N2"), p.Description ?? "", p.IsActive ? "نشط" : "موقوف"); _grid.Rows[i].Tag = p; } }
    private async Task Edit(SubscriptionPlanDto? item)
    {
        var name = new TextBox { Text = item?.Name ?? "" }; var fee = new NumericUpDown { Maximum = 10000000, DecimalPlaces = 2, Value = item?.DefaultFee ?? 0 }; var description = new TextBox { Text = item?.Description ?? "" };
        using var d = new ResponsiveForm { Text = "بيانات الخطة", Width = 430, Height = 260, StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
        var save = new Button { Text = "حفظ", DialogResult = DialogResult.OK, Left = 285, Top = 165 };
        d.Controls.AddRange([new Label { Text = "الاسم", Left = 20, Top = 20 }, name, new Label { Text = "الرسوم", Left = 20, Top = 65 }, fee, new Label { Text = "الوصف", Left = 20, Top = 110 }, description, save]);
        name.SetBounds(120, 18, 260, 28); fee.SetBounds(120, 63, 260, 28); description.SetBounds(120, 108, 260, 28);
        if (d.ShowDialog(this) != DialogResult.OK) return;
        await using var s = _scopes.CreateAsyncScope(); var service = s.ServiceProvider.GetRequiredService<ISubscriptionPlanService>(); var dto = new SaveSubscriptionPlanDto(name.Text, fee.Value, description.Text);
        var result = item is null ? await service.CreateAsync(dto) : await service.UpdateAsync(item.Id, dto); if (result.IsFailure) AppDialog.ShowInformation(this, Text, result.Error.Description); await LoadRows();
    }
}
