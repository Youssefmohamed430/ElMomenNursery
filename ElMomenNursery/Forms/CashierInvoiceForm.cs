using System.Diagnostics;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Controls;
using ElMomenNursery.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace ElMomenNursery.Forms;

internal sealed class CashierInvoiceForm : Form
{
    private sealed record CartLine(Guid LineId, Guid? ItemId, string Name, decimal Price, int Quantity)
    {
        public decimal Total => Price * Quantity;
    }

    private readonly IServiceScopeFactory _scopes;
    private readonly ComboBox _items = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly NumericUpDown _itemQuantity = new() { Minimum = 1, Maximum = 9999, Value = 1, Width = 100, ThousandsSeparator = true };
    private readonly TextBox _manualName = new() { Dock = DockStyle.Fill, PlaceholderText = "مثال: مدفوع آخر – اكتب وصفه" };
    private readonly NumericUpDown _manualPrice = new() { Minimum = 0, Maximum = 10000000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 125 };
    private readonly NumericUpDown _manualQuantity = new() { Minimum = 1, Maximum = 9999, Value = 1, Width = 100, ThousandsSeparator = true };
    private readonly TextBox _customer = new() { Dock = DockStyle.Fill, PlaceholderText = "اكتب اسم العميل" };
    private readonly ComboBox _method = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox _reference = new() { Dock = DockStyle.Fill, PlaceholderText = "اختياري" };
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RightToLeft = RightToLeft.Yes,
        RowHeadersVisible = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
        GridColor = BrandPalette.Border, RowTemplate = { Height = 36 }, ColumnHeadersHeight = 40,
        EnableHeadersVisualStyles = false
    };
    private readonly Label _total = new() { AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = BrandPalette.PrimaryGreen, Text = "الإجمالي: 0.00 ج.م" };
    private readonly List<CartLine> _cart = [];
    private List<InventoryItemDto> _catalog = [];

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
        ConfigureGrid();

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 5, BackColor = BrandPalette.AppBackground };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
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

        var save = Button("إصدار الفاتورة", primary: true);
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
        var rows = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(8, 3, 8, 5), BackColor = Color.White };
        rows.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        rows.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var stock = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1 };
        stock.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        stock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stock.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
        stock.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        stock.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        var addStock = Button("إضافة من المخزن", primary: true);
        stock.Controls.Add(FieldLabel("منتجات المخزن"), 0, 0);
        stock.Controls.Add(_items, 1, 0);
        stock.Controls.Add(FieldLabel("الكمية"), 2, 0);
        stock.Controls.Add(_itemQuantity, 3, 0);
        stock.Controls.Add(addStock, 4, 0);

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

        rows.Controls.Add(stock, 0, 0);
        rows.Controls.Add(manual, 0, 1);
        section.Controls.Add(rows);
        addStock.Click += (_, _) => AddCatalogItem();
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
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IInvoiceService>().GetAvailableItemsAsync();
            if (!ResultPresenter.Present(this, "منتجات المخزن", result)) return;
            _catalog = result.Value.ToList();
            foreach (var item in _catalog) _items.Items.Add(new ItemChoice(item));
            if (_items.Items.Count > 0) _items.SelectedIndex = 0;
        }
        catch { AppDialog.ShowInformation(this, Text, "تعذر تحميل منتجات المخزن."); }
    }

    private void AddCatalogItem()
    {
        if (_items.SelectedItem is not ItemChoice choice) return;
        var item = choice.Item;
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

    private async Task SaveAsync()
    {
        if (_cart.Count == 0)
        {
            AppDialog.ShowInformation(this, Text, "أضف صنفًا واحدًا على الأقل إلى الفاتورة.");
            return;
        }
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var dto = new CreateInvoiceDto(_customer.Text.Trim(), null,
                _cart.Select(x => new CreateInvoiceLineDto(x.ItemId, x.Name, x.Price, x.Quantity)).ToArray(),
                _method.SelectedIndex switch { 0 => PaymentMethod.Cash, 1 => PaymentMethod.BankTransfer, _ => PaymentMethod.Other },
                _reference.Text, null);
            var result = await scope.ServiceProvider.GetRequiredService<IInvoiceService>().CreateInvoiceAsync(dto);
            if (!ResultPresenter.Present(this, Text, result)) return;
            var pdf = await scope.ServiceProvider.GetRequiredService<IInvoicePdfService>().CreateInvoiceFileAsync(result.Value);
            if (ResultPresenter.Present(this, "الفاتورة", pdf))
                Process.Start(new ProcessStartInfo(pdf.Value) { UseShellExecute = true });
            DialogResult = DialogResult.OK;
            Close();
        }
        catch { AppDialog.ShowInformation(this, Text, "تعذر إصدار الفاتورة. تحقق من الكميات وحاول مرة أخرى."); }
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

    private sealed record ItemChoice(InventoryItemDto Item)
    {
        public override string ToString() => $"{Item.Name} — {Item.UnitPrice:N2} ج.م (المتاح {Item.Quantity})";
    }
}
