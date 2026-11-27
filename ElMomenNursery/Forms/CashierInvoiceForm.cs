using System.Diagnostics;
using System.Globalization;
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
    private sealed record ChildChoice(Guid Id, string Name)
    {
        public override string ToString() => Name;
    }

    private readonly IServiceScopeFactory _scopes;
    private readonly Guid? _initialChildId;
    private readonly string? _initialChildName;
    private readonly CategoryDrilldownPicker _categoryPicker = new() { Dock = DockStyle.Fill };
    private readonly TextBox _manualName = new() { Dock = DockStyle.Fill, PlaceholderText = "مثال: مدفوع آخر – اكتب وصفه" };
    private readonly NumericUpDown _manualPrice = new() { Minimum = 0, Maximum = 10000000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 125 };
    private readonly NumericUpDown _manualQuantity = new() { Minimum = 1, Maximum = 9999, Value = 1, Width = 100, ThousandsSeparator = true };
    private readonly TextBox _customer = new() { Dock = DockStyle.Fill, PlaceholderText = "اكتب اسم العميل" };
    private readonly ComboBox _child = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
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
    private readonly Dictionary<Guid, InventoryItemDto> _catalog = [];
    private readonly Button _saveButton = Button("إصدار الفاتورة", primary: true);

    public CashierInvoiceForm(IServiceScopeFactory scopes, Guid? initialChildId = null, string? initialChildName = null)
    {
        _scopes = scopes; _initialChildId = initialChildId; _initialChildName = initialChildName;
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
        _grid.CellEndEdit += (_, e) => UpdateCartLine(e.RowIndex, e.ColumnIndex);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 5, BackColor = BrandPalette.AppBackground };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 360));
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
        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 8, RowCount = 2, Padding = new Padding(8, 3, 8, 5), BackColor = Color.White };
        for (var i = 0; i < 4; i++) { fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25)); }
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        AddField(fields, "اسم العميل *", _customer, 0);
        AddField(fields, "ربط بملف طفل", _child, 2);
        AddField(fields, "طريقة الدفع", _method, 4);
        AddField(fields, "الرقم المرجعي", _reference, 6);
        _child.Items.Add("بدون ربط بملف طفل"); _child.SelectedIndex = 0;
        _child.SelectedIndexChanged += (_, _) => { if (_child.SelectedItem is ChildChoice selected) _customer.Text = selected.Name; };
        section.Controls.Add(fields);
        return section;
    }

    private Control BuildAddLinesSection()
    {
        var section = Section("إضافة بنود للفاتورة");
        var rows = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(8, 3, 8, 5), BackColor = Color.White };
        rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

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

        rows.Controls.Add(_categoryPicker, 0, 0); rows.Controls.Add(manual, 0, 1);
        section.Controls.Add(rows);
        _categoryPicker.ItemPicked += AddCatalogItem;
        addManual.Click += (_, _) => AddManualItem();
        return section;
    }

    private void ConfigureGrid()
    {
        _grid.Columns.AddRange([
            new DataGridViewTextBoxColumn { HeaderText = "الصنف / البيان", FillWeight = 45 },
            new DataGridViewTextBoxColumn { HeaderText = "الكمية", FillWeight = 15 },
            new DataGridViewTextBoxColumn { HeaderText = "سعر البيع (يمكن تعديله)", FillWeight = 20, ToolTipText = "يمكن تغيير سعر البيع لهذا البند دون تعديل سعر المخزن." },
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
        foreach (DataGridViewColumn column in _grid.Columns) column.ReadOnly = column.Index is not (1 or 2) || column.Visible == false;
        _grid.Columns[2].DefaultCellStyle.BackColor = BrandPalette.SoftBlue;
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
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
            var tree = await scope.ServiceProvider.GetRequiredService<IInventoryCategoryService>().GetCategoryTreeAsync(true);
            if (!ResultPresenter.Present(this, "فئات المخزن", tree)) return;
            _categoryPicker.LoadTree(tree.Value);
            _catalog.Clear(); AddItems(tree.Value);

            var children = await invoiceService.GetInvoiceChildrenAsync();
            if (children.IsSuccess)
                foreach (var child in children.Value) _child.Items.Add(new ChildChoice(child.Id, child.Name));
            if (_initialChildId is Guid childId)
            {
                _child.SelectedItem = _child.Items.Cast<object>().OfType<ChildChoice>().FirstOrDefault(x => x.Id == childId);
                if (_child.SelectedIndex < 0) _customer.Text = _initialChildName ?? string.Empty;
            }
        }, "تعذر تحميل منتجات المخزن.");
    }

    private void AddCatalogItem(InventoryItemDto item)
    {
        var quantity = _categoryPicker.PickedQuantity;
        var salePrice = _categoryPicker.PickedUnitPrice;
        var current = _cart.FirstOrDefault(x => x.ItemId == item.Id);
        var newQuantity = quantity + (current?.Quantity ?? 0);
        if (newQuantity > item.Quantity)
        {
            AppDialog.ShowInformation(this, Text, $"الكمية المتاحة من «{item.Name}» هي {item.Quantity}.");
            return;
        }
        _cart.RemoveAll(x => x.ItemId == item.Id);
        _cart.Add(new CartLine(Guid.NewGuid(), item.Id, item.Name, current?.Price ?? salePrice, newQuantity));
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

    private void UpdateCartLine(int rowIndex, int columnIndex)
    {
        if (columnIndex is not (1 or 2) || rowIndex < 0 || rowIndex >= _grid.Rows.Count) return;
        var lineId = (Guid)_grid.Rows[rowIndex].Cells[5].Value!;
        var line = _cart.FirstOrDefault(x => x.LineId == lineId);
        if (line is null) return;
        if (!int.TryParse(Convert.ToString(_grid.Rows[rowIndex].Cells[1].Value), out var quantity) || quantity <= 0)
        {
            AppDialog.ShowInformation(this, Text, "الكمية يجب أن تكون رقمًا أكبر من صفر."); RenderCart(); return;
        }
        if (line.ItemId is Guid itemId && FindCatalogItem(itemId) is { } item && quantity > item.Quantity)
        {
            AppDialog.ShowInformation(this, Text, $"الكمية المتاحة من «{item.Name}» هي {item.Quantity}."); RenderCart(); return;
        }
        if (!decimal.TryParse(Convert.ToString(_grid.Rows[rowIndex].Cells[2].Value), NumberStyles.Number, CultureInfo.CurrentCulture, out var price)
            || price < 0 || decimal.Round(price, 2) != price)
        {
            AppDialog.ShowInformation(this, Text, "سعر الوحدة يجب أن يكون صفرًا أو أكثر وبحد أقصى منزلتين عشريتين."); RenderCart(); return;
        }
        var index = _cart.FindIndex(x => x.LineId == lineId);
        _cart[index] = line with { Quantity = quantity, Price = price };
        RenderCart();
    }

    private void AddItems(IEnumerable<InventoryCategoryNodeDto> nodes)
    {
        foreach (var node in nodes)
        {
            foreach (var item in node.Items ?? []) _catalog[item.Id] = item;
            AddItems(node.Children);
        }
    }
    private InventoryItemDto? FindCatalogItem(Guid itemId) => _catalog.GetValueOrDefault(itemId);

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
            var childId = (_child.SelectedItem as ChildChoice)?.Id;
            var dto = new CreateInvoiceDto(_customer.Text.Trim(), childId,
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
