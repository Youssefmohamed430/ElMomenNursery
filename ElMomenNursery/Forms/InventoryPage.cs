using System.Drawing;
using Serilog;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Controls;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;
using static ElMomenNursery.Forms.MainForm;

namespace ElMomenNursery.Forms;

public sealed class InventoryPage : UserControl
{
    private static readonly ILogger Log = Serilog.Log.ForContext<InventoryPage>();
    private readonly IServiceScopeFactory? _scopes;
    private readonly UserSession? _session;
    private readonly IExcelExportService? _excel;
    private Func<Task>? _refreshItems;
    private Func<Task>? _refreshCategories;
    private TabControl? _tabs;
    private int _categoryLoadVersion;

    public InventoryPage() : this(null, null, null) { }
    public InventoryPage(IServiceScopeFactory? scopes, UserSession? session, IExcelExportService? excel)
    {
        _scopes = scopes; _session = session; _excel = excel;
        Dock = DockStyle.Fill; RightToLeft = RightToLeft.Yes; BackColor = BrandPalette.AppBackground;
        Controls.Add(BuildContent());
    }

    private Control BuildContent()
    {
        var page = CreateDataPage("المخزن", "إدارة الأصناف والفئات والأسعار مع الاحتفاظ بسجل التعديلات.", false);
        var tabs = new TabControl { RightToLeft = RightToLeft.Yes, RightToLeftLayout = true, Font = new Font("Segoe UI", 10F) };
        var items = BuildItemsTab(); var categories = BuildCategoriesTab();
        _tabs = tabs;
        tabs.TabPages.Add(new TabPage("الأصناف") { Controls = { items } });
        tabs.TabPages.Add(new TabPage("الفئات") { Controls = { categories } });
        page.Controls.Add(tabs);
        page.Resize += (_, _) => { LayoutDataHeader(page); tabs.SetBounds(34, 118, page.ClientSize.Width - 68, Math.Max(320, page.ClientSize.Height - 146)); };
        LayoutDataHeader(page); tabs.SetBounds(34, 118, page.ClientSize.Width - 68, Math.Max(320, page.ClientSize.Height - 146));
        return page;
    }

    private Control BuildItemsTab()
    {
        var root = new Panel { Dock = DockStyle.Fill, BackColor = BrandPalette.AppBackground, Padding = new Padding(8) };
        var search = CreateSearchBox("بحث باسم الصنف"); var category = Picker("كل الفئات"); var status = Picker("الكل", "نشط", "موقوف");
        var min = PriceBox(); var max = PriceBox(); var apply = CreateActionButton("تطبيق"); var clear = CreateActionButton("مسح الفلاتر");
        var add = CreateActionButton("إضافة صنف", true); var edit = CreateActionButton("تعديل"); var toggle = CreateActionButton("تفعيل / إيقاف"); var delete = CreateActionButton("حذف");
        var deleted = CreateActionButton("عرض المحذوفات"); var restore = CreateActionButton("استعادة"); var export = CreateActionButton("تصدير Excel");
        var count = CountLabel(); var grid = CreateGrid(["الصنف", "الفئة", "الكمية (قطعة)", "سعر الوحدة", "الحالة"]);
        var items = new List<InventoryItemDto>(); var showingDeleted = false; var busy = false; var loadVersion = 0; var reloadPending = false;
        _refreshItems = LoadItems;
        var canManage = _session?.HasPermission("Inventory.Manage") == true;
        add.Visible = edit.Visible = toggle.Visible = delete.Visible = canManage;
        deleted.Visible = _session?.HasPermission("Inventory.Restore") == true; restore.Visible = false;
        export.Visible = _session?.HasPermission("Inventory.Export") == true;
        category.Width = 180; status.Width = 130; min.Width = max.Width = 100;
        var filters = new FilterCard { Name = "inventory.items.filters" };
        filters.AddField("inventory.items.search", "اسم الصنف", search, 2);
        filters.AddField("inventory.items.category", "الفئة", category);
        filters.AddField("inventory.items.status", "الحالة", status);
        filters.AddField("inventory.items.min", "أقل سعر", min);
        filters.AddField("inventory.items.max", "أعلى سعر", max);
        filters.AddActions(apply, clear);
        var actions = new ActionToolbar { Name = "inventory.items.actions" };
        actions.Add(add, edit, toggle, delete, deleted, restore, export);
        count.Dock = DockStyle.None; count.Height = 30; grid.Dock = DockStyle.None;
        root.Controls.Add(grid); root.Controls.Add(count); root.Controls.Add(actions); root.Controls.Add(filters);
        void LayoutItems()
        {
            var width = Math.Max(240, root.ClientSize.Width - root.Padding.Horizontal); filters.SetBounds(0, 0, width, filters.Height); filters.Arrange();
            actions.SetBounds(0, filters.Bottom + 8, width, actions.ArrangeItems(width)); count.SetBounds(0, actions.Bottom, width, 28);
            grid.SetBounds(0, count.Bottom, width, Math.Max(120, root.ClientSize.Height - count.Bottom));
        }
        root.Resize += (_, _) => LayoutItems(); LayoutItems();

        apply.Click += async (_, _) => await LoadItems(); clear.Click += async (_, _) => { search.Clear(); category.SelectedIndex = 0; status.SelectedIndex = 0; min.Clear(); max.Clear(); await LoadItems(); };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await LoadItems(); } };
        add.Click += async (_, _) => { using var form = new InventoryItemEditorForm(_scopes); if (form.ShowDialog(this) == DialogResult.OK) await LoadItems(); };
        edit.Click += async (_, _) => await EditSelected(); grid.CellDoubleClick += async (_, _) => await EditSelected();
        toggle.Click += async (_, _) => await ToggleItem(); delete.Click += async (_, _) => await DeleteItem();
        deleted.Click += async (_, _) => { showingDeleted = !showingDeleted; loadVersion++; deleted.Text = showingDeleted ? "عرض الأصناف" : "عرض المحذوفات"; restore.Visible = showingDeleted && _session?.HasPermission("Inventory.Restore") == true; add.Visible = edit.Visible = toggle.Visible = delete.Visible = !showingDeleted && canManage; export.Visible = !showingDeleted && _session?.HasPermission("Inventory.Export") == true; filters.Enabled = !showingDeleted; await LoadItems(); };
        restore.Click += async (_, _) => await RestoreItem();
        export.Click += async (_, _) => await GridExcelExport.SaveAsync(this, grid, _excel!, "تصدير الأصناف", "الأصناف");
        if (_scopes is not null) _ = LoadLookups();
        if (_scopes is not null) _ = LoadItems();

        async Task LoadLookups()
        {
            try
            {
                await using var scope = _scopes!.CreateAsyncScope();
                var result = await scope.ServiceProvider.GetRequiredService<IInventoryCategoryService>().GetCategoryLookupAsync(false);
                if (IsDisposed || category.IsDisposed || result.IsFailure) return;
                category.Items.Clear(); category.Items.Add("كل الفئات");
                foreach (var option in result.Value) category.Items.Add(new CategoryChoice(option.Id, option.Name));
                category.SelectedIndex = 0;
            }
            catch (Exception ex) { Log.Error(ex, "Failed to load inventory category filter"); }
        }
        async Task LoadItems()
        {
            if (_scopes is null) return; if (busy) { reloadPending = true; return; } busy = true; reloadPending = false; var version = ++loadVersion;
            try
            {
                Result<IReadOnlyList<InventoryItemDto>> result;
                await using (var scope = _scopes.CreateAsyncScope())
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IInventoryItemService>();
                    if (showingDeleted) result = await svc.GetDeletedItemsAsync();
                    else
                    {
                        decimal? minPrice = decimal.TryParse(min.Text, out var lo) ? lo : null;
                        decimal? maxPrice = decimal.TryParse(max.Text, out var hi) ? hi : null;
                        Guid? categoryId = category.SelectedItem is CategoryChoice choice ? choice.Id : null;
                        bool? active = status.SelectedIndex switch { 1 => true, 2 => false, _ => null };
                        result = await svc.GetItemsAsync(new InventoryItemFilter(search.Text, categoryId, active, minPrice, maxPrice));
                    }
                }
                if (IsDisposed || version != loadVersion) return;
                if (result.IsFailure) { ResultPresenter.Present(this, "المخزن", result); return; }
                items = result.Value.ToList(); Render();
            }
            catch (Exception ex) { Log.Error(ex, "Inventory item list failed"); if (!IsDisposed) AppDialog.ShowInformation(this, "المخزن", "تعذر تحميل الأصناف. أعد المحاولة."); }
            finally { busy = false; if (reloadPending && !IsDisposed) { reloadPending = false; _ = LoadItems(); } }
        }
        void Render()
        {
            grid.Rows.Clear();
            foreach (var row in items)
            {
                var idx = grid.Rows.Add(row.Name, row.CategoryName, row.Quantity.ToString("N0"), row.UnitPrice.ToString("N2") + " ج.م", row.IsActive ? "نشط" : "موقوف");
                grid.Rows[idx].Tag = row.Id;
            }
            count.Text = $"عدد الأصناف: {items.Count}";
        }
        InventoryItemDto? Selected() => grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Guid id ? items.FirstOrDefault(x => x.Id == id) : null;
        async Task EditSelected()
        {
            var row = Selected(); if (row is null) { AppDialog.ShowInformation(this, "المخزن", "اختر صنفًا من الجدول أولًا."); return; }
            using var form = new InventoryItemEditorForm(_scopes, row); if (form.ShowDialog(this) == DialogResult.OK) await LoadItems();
        }
        async Task ToggleItem()
        {
            var row = Selected(); if (row is null) { AppDialog.ShowInformation(this, "المخزن", "اختر صنفًا من الجدول أولًا."); return; }
            await using var scope = _scopes!.CreateAsyncScope(); var result = await scope.ServiceProvider.GetRequiredService<IInventoryItemService>().SetItemActiveAsync(row.Id, !row.IsActive);
            if (ResultPresenter.Present(this, "حالة الصنف", result)) await LoadItems();
        }
        async Task DeleteItem()
        {
            var row = Selected(); if (row is null) { AppDialog.ShowInformation(this, "المخزن", "اختر صنفًا من الجدول أولًا."); return; }
            if (!AppDialog.Confirm(this, "حذف صنف", "هل تريد نقل الصنف المحدد إلى المحذوفات؟ سيختفي من القوائم النشطة مع الاحتفاظ بسجله التاريخي.")) return;
            await using var scope = _scopes!.CreateAsyncScope(); var result = await scope.ServiceProvider.GetRequiredService<IInventoryItemService>().DeleteItemAsync(row.Id);
            if (ResultPresenter.Present(this, "حذف صنف", result)) await LoadItems();
        }
        async Task RestoreItem()
        {
            var row = Selected(); if (row is null) { AppDialog.ShowInformation(this, "المخزن", "اختر صنفًا من المحذوفات أولًا."); return; }
            await using var scope = _scopes!.CreateAsyncScope(); var result = await scope.ServiceProvider.GetRequiredService<IInventoryItemService>().RestoreItemAsync(row.Id);
            if (ResultPresenter.Present(this, "استعادة صنف", result)) await LoadItems();
        }
        return root;
    }

    private Control BuildCategoriesTab()
    {
        var root = new Panel { Dock = DockStyle.Fill, BackColor = BrandPalette.AppBackground, Padding = new Padding(8) };
        var search = CreateSearchBox("بحث باسم الفئة"); var status = Picker("الكل", "نشط", "موقوف");
        var apply = CreateActionButton("تطبيق"); var clear = CreateActionButton("مسح الفلاتر");
        var add = CreateActionButton("إضافة فئة", true); var edit = CreateActionButton("تعديل"); var toggle = CreateActionButton("تفعيل / إيقاف"); var delete = CreateActionButton("حذف");
        var deleted = CreateActionButton("عرض المحذوفات"); var restore = CreateActionButton("استعادة"); var export = CreateActionButton("تصدير Excel");
        var count = CountLabel(); var grid = CreateGrid(["الفئة", "الوصف", "عدد الأصناف", "الحالة"]);
        var rows = new List<InventoryCategoryDto>(); var showingDeleted = false; var busy = false; var categoryReloadPending = false;
        _refreshCategories = Load;
        var canManage = _session?.HasPermission("Inventory.Manage") == true;
        add.Visible = edit.Visible = toggle.Visible = delete.Visible = canManage; deleted.Visible = _session?.HasPermission("Inventory.Restore") == true;
        restore.Visible = false; export.Visible = _session?.HasPermission("Inventory.Export") == true;
        status.Width = 130;
        var filters = new FilterCard { Name = "inventory.categories.filters" };
        filters.AddField("inventory.categories.search", "اسم الفئة", search, 2);
        filters.AddField("inventory.categories.status", "الحالة", status);
        filters.AddActions(apply, clear);
        var actions = new ActionToolbar { Name = "inventory.categories.actions" };
        actions.Add(add, edit, toggle, delete, deleted, restore, export);
        count.Dock = DockStyle.None; count.Height = 30; grid.Dock = DockStyle.None;
        root.Controls.Add(grid); root.Controls.Add(count); root.Controls.Add(actions); root.Controls.Add(filters);
        void LayoutCategories()
        {
            var width = Math.Max(240, root.ClientSize.Width - root.Padding.Horizontal); filters.SetBounds(0, 0, width, filters.Height); filters.Arrange();
            actions.SetBounds(0, filters.Bottom + 8, width, actions.ArrangeItems(width)); count.SetBounds(0, actions.Bottom, width, 28);
            grid.SetBounds(0, count.Bottom, width, Math.Max(120, root.ClientSize.Height - count.Bottom));
        }
        root.Resize += (_, _) => LayoutCategories(); LayoutCategories();
        apply.Click += async (_, _) => await Load(); clear.Click += async (_, _) => { search.Clear(); status.SelectedIndex = 0; await Load(); };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await Load(); } };
        add.Click += async (_, _) => { using var form = new InventoryCategoryEditorForm(_scopes); if (form.ShowDialog(this) == DialogResult.OK) await Load(); };
        edit.Click += async (_, _) => await Edit(); grid.CellDoubleClick += async (_, _) => await Edit();
        toggle.Click += async (_, _) => await Toggle(); delete.Click += async (_, _) => await Delete();
        deleted.Click += async (_, _) => { showingDeleted = !showingDeleted; _categoryLoadVersion++; deleted.Text = showingDeleted ? "عرض الفئات" : "عرض المحذوفات"; restore.Visible = showingDeleted && _session?.HasPermission("Inventory.Restore") == true; add.Visible = edit.Visible = toggle.Visible = delete.Visible = !showingDeleted && canManage; export.Visible = !showingDeleted && _session?.HasPermission("Inventory.Export") == true; filters.Enabled = !showingDeleted; await Load(); };
        restore.Click += async (_, _) => await Restore();
        export.Click += async (_, _) => await GridExcelExport.SaveAsync(this, grid, _excel!, "تصدير الفئات", "الفئات");
        if (_scopes is not null) _ = Load();

        async Task Load()
        {
            if (_scopes is null) return; if (busy) { categoryReloadPending = true; return; } busy = true; categoryReloadPending = false; var version = ++_categoryLoadVersion;
            try
            {
                Result<IReadOnlyList<InventoryCategoryDto>> result;
                await using (var scope = _scopes.CreateAsyncScope())
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IInventoryCategoryService>();
                    result = showingDeleted ? await svc.GetDeletedCategoriesAsync() : await svc.GetCategoriesAsync(new InventoryCategoryFilter(search.Text, status.SelectedIndex switch { 1 => true, 2 => false, _ => null }));
                }
                if (IsDisposed || grid.IsDisposed || version != _categoryLoadVersion) return;
                if (result.IsFailure) { ResultPresenter.Present(this, "الفئات", result); return; }
                rows = result.Value.ToList(); grid.Rows.Clear();
                foreach (var row in rows) { var idx = grid.Rows.Add(row.Name, row.Description ?? string.Empty, row.ItemsCount, row.IsActive ? "نشط" : "موقوف"); grid.Rows[idx].Tag = row.Id; }
                count.Text = $"عدد الفئات: {rows.Count}";
            }
            catch (Exception ex) { Log.Error(ex, "Inventory category list failed"); if (!IsDisposed) AppDialog.ShowInformation(this, "المخزن", "تعذر تحميل الفئات. أعد المحاولة."); }
            finally { busy = false; if (categoryReloadPending && !IsDisposed) { categoryReloadPending = false; _ = Load(); } }
        }
        InventoryCategoryDto? Selected() => grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is Guid id ? rows.FirstOrDefault(x => x.Id == id) : null;
        async Task Edit() { var row = Selected(); if (row is null) { AppDialog.ShowInformation(this, "المخزن", "اختر فئة من الجدول أولًا."); return; } using var form = new InventoryCategoryEditorForm(_scopes, row); if (form.ShowDialog(this) == DialogResult.OK) await Load(); }
        async Task Toggle() { var row = Selected(); if (row is null) { AppDialog.ShowInformation(this, "المخزن", "اختر فئة من الجدول أولًا."); return; } await using var scope = _scopes!.CreateAsyncScope(); var result = await scope.ServiceProvider.GetRequiredService<IInventoryCategoryService>().SetCategoryActiveAsync(row.Id, !row.IsActive); if (ResultPresenter.Present(this, "حالة الفئة", result)) await Load(); }
        async Task Delete() { var row = Selected(); if (row is null) { AppDialog.ShowInformation(this, "المخزن", "اختر فئة من الجدول أولًا."); return; } if (!AppDialog.Confirm(this, "حذف فئة", "هل تريد نقل الفئة إلى المحذوفات؟ إذا كانت تحتوي على أصناف، يمكنك إيقافها بدلًا من حذفها.")) return; await using var scope = _scopes!.CreateAsyncScope(); var result = await scope.ServiceProvider.GetRequiredService<IInventoryCategoryService>().DeleteCategoryAsync(row.Id); if (ResultPresenter.Present(this, "حذف فئة", result)) await Load(); }
        async Task Restore() { var row = Selected(); if (row is null) { AppDialog.ShowInformation(this, "المخزن", "اختر فئة من المحذوفات أولًا."); return; } await using var scope = _scopes!.CreateAsyncScope(); var result = await scope.ServiceProvider.GetRequiredService<IInventoryCategoryService>().RestoreCategoryAsync(row.Id); if (ResultPresenter.Present(this, "استعادة فئة", result)) await Load(); }
        return root;
    }

    private static ComboBox Picker(params string[] values) { var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), RightToLeft = RightToLeft.Yes }; c.Items.AddRange(values); c.SelectedIndex = 0; return c; }
    private static TextBox PriceBox() => new() { PlaceholderText = "السعر", Width = 100, Font = new Font("Segoe UI", 10F), RightToLeft = RightToLeft.Yes };
    private static Label CountLabel() => new() { ForeColor = BrandPalette.MutedText, TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes };
    private sealed record CategoryChoice(Guid Id, string Name) { public override string ToString() => Name; }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5)
        {
            var refresh = _tabs?.SelectedIndex == 1 ? _refreshCategories : _refreshItems;
            if (refresh is not null) { _ = refresh(); return true; }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}
