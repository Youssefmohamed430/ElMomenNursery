using System.Diagnostics;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Controls;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ElMomenNursery.Forms;

internal sealed class CashierInvoiceForm : ResponsiveForm
{
    private sealed record CartLine(Guid LineId, Guid? ItemId, string Name, decimal Price, int Quantity)
    {
        public decimal Total => Price * Quantity;
    }

    private readonly IServiceScopeFactory _scopes;
    private readonly TextBox _productSearch = new() { Dock = DockStyle.Fill, PlaceholderText = "ابحث عن منتج في كل الفئات..." };
    private readonly FlowLayoutPanel _categoryCards = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(4) };
    private readonly FlowLayoutPanel _productCards = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(4) };
    private readonly FlowLayoutPanel _breadcrumbs = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft };
    private readonly Label _browserState = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = BrandPalette.MutedText };
    private readonly NumericUpDown _itemQuantity = new() { Minimum = 1, Maximum = 9999, Value = 1, Width = 100, ThousandsSeparator = true };
    private readonly TextBox _manualName = new() { Dock = DockStyle.Fill, PlaceholderText = "مثال: مدفوع آخر – اكتب وصفه" };
    private readonly NumericUpDown _manualPrice = new() { Minimum = 0, Maximum = 10000000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 125 };
    private readonly NumericUpDown _manualQuantity = new() { Minimum = 1, Maximum = 9999, Value = 1, Width = 100, ThousandsSeparator = true };
    private readonly TextBox _customer = new() { Dock = DockStyle.Fill, PlaceholderText = "اكتب اسم العميل" };
    private readonly ComboBox _method = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox _reference = new() { Dock = DockStyle.Fill, PlaceholderText = "اختياري" };
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RightToLeft = RightToLeft.Yes,
        RowHeadersVisible = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
        GridColor = BrandPalette.Border, RowTemplate = { Height = 36 }, ColumnHeadersHeight = 40,
        EnableHeadersVisualStyles = false
    };
    private readonly Label _total = new() { AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = BrandPalette.PrimaryGreen, Text = "الإجمالي: 0.00 ج.م" };
    private readonly List<CartLine> _cart = [];
    private readonly Button _saveButton = Button("إصدار الفاتورة", primary: true);
    private List<InventoryCategoryLookupDto> _categories = [];
    private readonly List<InventoryCategoryLookupDto> _categoryPath = [];
    private readonly Dictionary<Guid, InventoryItemDto> _productCache = [];
    private readonly Dictionary<Guid, IReadOnlyList<InventoryItemDto>> _categoryProductCache = [];
    private int _browserRequest;

    public CashierInvoiceForm(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
        Text = "فاتورة بيع جديدة";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 650);
        Size = new Size(1120, 790);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = BrandPalette.AppBackground;

        _method.Items.AddRange(["نقدًا", "تحويل بنكي", "أخرى"]);
        _method.SelectedIndex = 0;
        Shown += async (_, _) => await LookupOptions.LoadAsync(_scopes, _method, "PaymentMethod", ["نقدًا", "تحويل بنكي", "أخرى"]);
        ConfigureGrid();
        _grid.CellEndEdit += (_, e) => UpdateCartQuantity(e.RowIndex, e.ColumnIndex);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 5, BackColor = BrandPalette.AppBackground };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 350));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        var details = BuildDetailsSection();
        var addLines = BuildAddLinesSection();
        var cartTools = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(2, 8, 2, 0) };
        cartTools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cartTools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var remove = Button("حذف البند المحدد");
        cartTools.Controls.Add(_total, 0, 0);
        cartTools.Controls.Add(remove, 1, 0);

        var save = _saveButton;
        var cancel = Button("إلغاء");
        cancel.DialogResult = DialogResult.Cancel;
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 5, 0, 0) };
        footer.Controls.AddRange([cancel, save]);

        root.Controls.Add(details, 0, 0);
        root.Controls.Add(addLines, 0, 1);
        root.Controls.Add(_grid, 0, 2);
        root.Controls.Add(cartTools, 0, 3);
        root.Controls.Add(footer, 0, 4);
        Controls.Add(root);
        CancelButton = cancel;

        remove.Click += (_, _) => RemoveSelected();
        save.Click += async (_, _) => await SaveAsync();
        Load += async (_, _) => await LoadCatalogAsync();
    }

    private Control BuildDetailsSection()
    {
        var section = Section("بيانات الفاتورة");
        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 2, Padding = new Padding(8, 3, 8, 5), BackColor = Color.White };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        AddField(fields, "اسم العميل *", _customer, 0);
        AddField(fields, "طريقة الدفع", _method, 2);
        AddField(fields, "الرقم المرجعي", _reference, 4);
        section.Controls.Add(fields);
        return section;
    }

    private Control BuildAddLinesSection()
    {
        var section = Section("إضافة بنود للفاتورة");
        var rows = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(8, 3, 8, 5), BackColor = Color.White };
        rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var searchRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95)); searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60)); searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        searchRow.Controls.Add(FieldLabel("بحث عام"), 0, 0); searchRow.Controls.Add(_productSearch, 1, 0);
        searchRow.Controls.Add(FieldLabel("الكمية"), 2, 0); searchRow.Controls.Add(_itemQuantity, 3, 0);
        var nav = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var back = Button("رجوع"); back.Click += (_, _) => { if (_categoryPath.Count > 0) _categoryPath.RemoveAt(_categoryPath.Count - 1); _ = LoadBrowserAsync(); };
        nav.Controls.Add(back, 0, 0); nav.Controls.Add(_breadcrumbs, 1, 0);
        _categoryCards.FlowDirection = FlowDirection.TopDown; _categoryCards.WrapContents = false;
        _categoryCards.Margin = new Padding(0, 0, 6, 0); _productCards.Margin = new Padding(6, 0, 0, 0);
        var browser = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(2, 4, 2, 0) };
        browser.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); browser.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        var categoryPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(4), BackColor = Color.FromArgb(247, 249, 248) };
        categoryPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); categoryPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        categoryPanel.Controls.Add(new Label { Text = "الفئات", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font(Font, FontStyle.Bold), ForeColor = BrandPalette.PrimaryGreen }, 0, 0);
        categoryPanel.Controls.Add(_categoryCards, 0, 1);
        var productPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(4), BackColor = Color.White };
        productPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); productPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        productPanel.Controls.Add(_browserState, 0, 0); productPanel.Controls.Add(_productCards, 0, 1);
        browser.Controls.Add(categoryPanel, 0, 0); browser.Controls.Add(productPanel, 1, 0);

        var manual = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, RowCount = 1 };
        manual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        manual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        manual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45));
        manual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        manual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        manual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        manual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        var addManual = Button("إضافة بند يدوي");
        manual.Controls.Add(FieldLabel("بند غير مخزني"), 0, 0);
        manual.Controls.Add(_manualName, 1, 0);
        manual.Controls.Add(FieldLabel("السعر"), 2, 0);
        manual.Controls.Add(_manualPrice, 3, 0);
        manual.Controls.Add(FieldLabel("الكمية"), 4, 0);
        manual.Controls.Add(_manualQuantity, 5, 0);
        manual.Controls.Add(addManual, 6, 0);

        rows.Controls.Add(searchRow, 0, 0); rows.Controls.Add(nav, 0, 1); rows.Controls.Add(browser, 0, 2);
        var lower = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 }; lower.Controls.Add(manual, 0, 0);
        rows.RowStyles[0] = new RowStyle(SizeType.Absolute, 40); rows.RowStyles[1] = new RowStyle(SizeType.Absolute, 36); rows.RowStyles[2] = new RowStyle(SizeType.Percent, 100);
        rows.RowCount = 4; rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        rows.Controls.Add(lower, 0, 3);
        section.Controls.Add(rows);
        _productSearch.TextChanged += async (_, _) => await SearchProductsAsync();
        addManual.Click += (_, _) => AddManualItem();
        return section;
    }

    private void ConfigureGrid()
    {
        _grid.Columns.AddRange([
            new DataGridViewTextBoxColumn { HeaderText = "الصنف / البيان", FillWeight = 45 },
            new DataGridViewTextBoxColumn { HeaderText = "الكمية", FillWeight = 15 },
            new DataGridViewTextBoxColumn { HeaderText = "سعر الوحدة", FillWeight = 20 },
            new DataGridViewTextBoxColumn { HeaderText = "الإجمالي", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Visible = false },
            new DataGridViewTextBoxColumn { Visible = false }
        ]);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = BrandPalette.SoftGreen;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = BrandPalette.Text;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        _grid.DefaultCellStyle.SelectionBackColor = BrandPalette.SoftBlue;
        _grid.DefaultCellStyle.SelectionForeColor = BrandPalette.Text;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 249);
        foreach (DataGridViewColumn column in _grid.Columns) column.ReadOnly = column.Index != 1;
    }

    private static GroupBox Section(string title) => new()
    {
        Text = title, Dock = DockStyle.Fill, Padding = new Padding(8, 5, 8, 7),
        BackColor = Color.White, ForeColor = BrandPalette.PrimaryGreen, RightToLeft = RightToLeft.Yes
    };

    private static void AddField(TableLayoutPanel panel, string title, Control control, int column)
    {
        panel.Controls.Add(FieldLabel(title), column, 0);
        panel.Controls.Add(control, column + 1, 1);
    }

    private static Label FieldLabel(string title) => new()
    {
        Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight,
        ForeColor = BrandPalette.Text, RightToLeft = RightToLeft.Yes, AutoEllipsis = true
    };

    private async Task LoadCatalogAsync()
    {
        await UiGuard.RunAsync(this, "CashierInvoice.LoadCatalog", async () =>
        {
            await using var scope = _scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IInvoiceService>().GetInvoiceCategoriesAsync();
            if (!ResultPresenter.Present(this, "فئات المخزن", result)) return;
            _categories = result.Value.ToList();
            await LoadBrowserAsync();
        }, "تعذر تحميل منتجات المخزن.");
    }

    private async Task LoadBrowserAsync()
    {
        var request = ++_browserRequest;
        _categoryCards.Controls.Clear(); _productCards.Controls.Clear(); _breadcrumbs.Controls.Clear();
        var searching = !string.IsNullOrWhiteSpace(_productSearch.Text);
        var current = _categoryPath.LastOrDefault();
        var crumbs = new List<(string Text, int Count)> { ("كل الفئات", 0) };
        for (var i = 0; i < _categoryPath.Count; i++) crumbs.Add((_categoryPath[i].Name, i + 1));
        foreach (var crumb in crumbs)
        {
            var crumbButton = Button(crumb.Text); crumbButton.Height = 28; crumbButton.MinimumSize = new Size(75,28);
            var depth = crumb.Count; crumbButton.Click += (_, _) => { _categoryPath.RemoveRange(depth, _categoryPath.Count - depth); _ = LoadBrowserAsync(); };
            _breadcrumbs.Controls.Add(crumbButton);
        }
        _browserState.Text = searching ? "نتائج البحث في كل الفئات" : current?.Name ?? "اختر فئة أو منتجًا";
        if (!searching)
            foreach (var category in _categories.Where(x => x.ParentCategoryId == current?.Id).OrderBy(x => x.Name))
            {
                var childCount = _categories.Count(x => x.ParentCategoryId == category.Id);
            var button = BrowserCard(category.Name, childCount > 0 ? $"{childCount} فئة فرعية" : "عرض المنتجات"); button.Height = 60; button.MinimumSize = new Size(120, 60); button.Width = 220; button.Click += (_, _) => { _categoryPath.Add(category); _ = LoadBrowserAsync(); };
                _categoryCards.Controls.Add(button);
            }
        IReadOnlyList<InventoryItemDto> products = [];
        if (searching || current is not null)
        {
            if (!searching && current is not null && _categoryProductCache.TryGetValue(current.Id, out var cached)) products = cached;
            else
            {
                await using var scope = _scopes.CreateAsyncScope();
                var result = await scope.ServiceProvider.GetRequiredService<IInvoiceService>().BrowseAvailableItemsAsync(searching ? null : current?.Id, searching ? _productSearch.Text : null);
                if (request != _browserRequest || IsDisposed) return;
                if (result.IsFailure) { _browserState.Text = result.Error.Description; return; }
                products = result.Value;
                if (!searching && current is not null) _categoryProductCache[current.Id] = products;
            }
        }
        foreach (var item in products)
        {
            _productCache[item.Id] = item;
            var button = BrowserCard(item.Name, $"{item.UnitPrice:N2} ج.م  |  المتاح: {item.Quantity}");
            button.Enabled = item.Quantity > 0; button.Click += (_, _) => AddCatalogItem(item); _productCards.Controls.Add(button);
        }
        if (_categoryCards.Controls.Count == 0 && _productCards.Controls.Count == 0)
            _browserState.Text = searching ? "لا توجد منتجات مطابقة." : "لا توجد فئات أو منتجات في هذا المستوى.";
    }

    private async Task SearchProductsAsync()
    {
        var value = _productSearch.Text;
        await Task.Delay(250);
        if (value == _productSearch.Text && !IsDisposed) await LoadBrowserAsync();
    }

    private static Button BrowserCard(string title, string subtitle)
    {
        var button = Button($"{title}\n{subtitle}"); button.Size = new Size(185, 70); button.MinimumSize = new Size(185,70);
        button.TextAlign = ContentAlignment.MiddleCenter; button.Margin = new Padding(5); button.UseVisualStyleBackColor = true;
        return button;
    }

    private void AddCatalogItem(InventoryItemDto item)
    {
        var quantity = (int)_itemQuantity.Value;
        var current = _cart.FirstOrDefault(x => x.ItemId == item.Id);
        var newQuantity = quantity + (current?.Quantity ?? 0);
        if (newQuantity > item.Quantity)
        {
            AppDialog.ShowInformation(this, Text, $"الكمية المتاحة من «{item.Name}» هي {item.Quantity}.");
            return;
        }
        _cart.RemoveAll(x => x.ItemId == item.Id);
        _cart.Add(new CartLine(Guid.NewGuid(), item.Id, item.Name, item.UnitPrice, newQuantity));
        RenderCart();
    }

    private void AddManualItem()
    {
        if (string.IsNullOrWhiteSpace(_customer.Text))
        {
            AppDialog.ShowInformation(this, Text, "اسم العميل مطلوب لإصدار الفاتورة.");
            _customer.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(_manualName.Text))
        {
            AppDialog.ShowInformation(this, Text, "اكتب وصف البند اليدوي.");
            _manualName.Focus();
            return;
        }
        _cart.Add(new CartLine(Guid.NewGuid(), null, _manualName.Text.Trim(), _manualPrice.Value, (int)_manualQuantity.Value));
        _manualName.Clear();
        _manualPrice.Value = 0;
        _manualQuantity.Value = 1;
        RenderCart();
    }

    private void RemoveSelected()
    {
        if (_grid.SelectedRows.Count == 0) return;
        var id = (Guid)_grid.SelectedRows[0].Cells[5].Value!;
        _cart.RemoveAll(x => x.LineId == id);
        RenderCart();
    }

    private void RenderCart()
    {
        _grid.Rows.Clear();
        foreach (var line in _cart)
            _grid.Rows.Add(line.Name, line.Quantity, line.Price.ToString("N2"), line.Total.ToString("N2"), line.ItemId ?? (object)DBNull.Value, line.LineId);
        _total.Text = $"الإجمالي: {_cart.Sum(x => x.Total):N2} ج.م";
    }

    private void UpdateCartQuantity(int rowIndex, int columnIndex)
    {
        if (columnIndex != 1 || rowIndex < 0 || rowIndex >= _grid.Rows.Count) return;
        var lineId = (Guid)_grid.Rows[rowIndex].Cells[5].Value!;
        var line = _cart.FirstOrDefault(x => x.LineId == lineId);
        if (line is null) return;
        if (!int.TryParse(Convert.ToString(_grid.Rows[rowIndex].Cells[1].Value), out var quantity) || quantity <= 0)
        {
            AppDialog.ShowInformation(this, Text, "الكمية يجب أن تكون رقمًا أكبر من صفر."); RenderCart(); return;
        }
        if (line.ItemId is Guid itemId && _productCache.TryGetValue(itemId, out var item) && quantity > item.Quantity)
        {
            AppDialog.ShowInformation(this, Text, $"الكمية المتاحة من «{item.Name}» هي {item.Quantity}."); RenderCart(); return;
        }
        var index = _cart.FindIndex(x => x.LineId == lineId);
        _cart[index] = line with { Quantity = quantity };
        RenderCart();
    }

    private async Task SaveAsync()
    {
        if (!_saveButton.Enabled) return;
        if (_cart.Count == 0)
        {
            AppDialog.ShowInformation(this, Text, "أضف صنفًا واحدًا على الأقل إلى الفاتورة.");
            return;
        }
        _saveButton.Enabled = false;
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var dto = new CreateInvoiceDto(_customer.Text.Trim(), null,
                _cart.Select(x => new CreateInvoiceLineDto(x.ItemId, x.Name, x.Price, x.Quantity)).ToArray(),
                _method.Text == "نقدًا" ? PaymentMethod.Cash : _method.Text == "تحويل بنكي" ? PaymentMethod.BankTransfer : PaymentMethod.Other,
                _reference.Text, _method.Text is not ("نقدًا" or "تحويل بنكي" or "أخرى") ? $"طريقة الدفع: {_method.Text}" : null);
            var result = await scope.ServiceProvider.GetRequiredService<IInvoiceService>().CreateInvoiceAsync(dto);
            if (!ResultPresenter.Present(this, Text, result)) return;
            try
            {
                var pdf = await scope.ServiceProvider.GetRequiredService<IInvoicePdfService>().CreateInvoiceFileAsync(result.Value);
                if (pdf.IsSuccess)
                    Process.Start(new ProcessStartInfo(pdf.Value) { UseShellExecute = true });
                else
                {
                    Log.ForContext<CashierInvoiceForm>().Warning("Invoice saved but PDF generation failed; invoiceId={InvoiceId}", result.Value.Id);
                    AppDialog.ShowInformation(this, "الفاتورة", "تم حفظ الفاتورة، لكن تعذر إنشاء ملف PDF.");
                }
            }
            catch (Exception ex)
            {
                Log.ForContext<CashierInvoiceForm>().Error(ex, "Invoice PDF creation or opening failed");
                AppDialog.ShowInformation(this, "الفاتورة", "تم حفظ الفاتورة، لكن تعذر إنشاء ملف PDF أو فتحه.");
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) { Log.ForContext<CashierInvoiceForm>().Error(ex, "Invoice save failed"); AppDialog.ShowInformation(this, Text, "تعذر إصدار الفاتورة. تحقق من الكميات وحاول مرة أخرى."); }
        finally { if (!IsDisposed) _saveButton.Enabled = true; }
    }

    private static Button Button(string text, bool primary = false) => new()
    {
        Text = text, AutoSize = true, Height = 36, MinimumSize = new Size(120, 36),
        UseVisualStyleBackColor = !primary,
        BackColor = primary ? BrandPalette.PrimaryGreen : SystemColors.Control,
        ForeColor = primary ? Color.White : BrandPalette.Text,
        FlatStyle = primary ? FlatStyle.Flat : FlatStyle.Standard,
        Anchor = AnchorStyles.Right
    };

}
