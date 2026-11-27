using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

internal sealed class CategoryDrilldownPicker : UserControl
{
    public event Action<InventoryItemDto>? ItemPicked;
    public decimal PickedUnitPrice { get; private set; }
    public int PickedQuantity { get; private set; } = 1;
    private readonly TextBox _search = new() { Dock = DockStyle.Top, PlaceholderText = "بحث في هذه الفئة" };
    private readonly FlowLayoutPanel _breadcrumb = new() { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, RightToLeft = RightToLeft.Yes, Padding = new Padding(4) };
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, RightToLeft = RightToLeft.Yes, Padding = new Padding(4) };
    private IReadOnlyList<InventoryCategoryNodeDto> _root = [];
    private readonly List<InventoryCategoryNodeDto> _path = [];

    public CategoryDrilldownPicker()
    {
        BackColor = Color.White; RightToLeft = RightToLeft.Yes;
        Controls.Add(_list); Controls.Add(_breadcrumb); Controls.Add(_search);
        _search.TextChanged += (_, _) => RenderCurrentLevel();
        Resize += (_, _) => RenderCurrentLevel();
    }

    public void LoadTree(IReadOnlyList<InventoryCategoryNodeDto> tree) { _root = tree; _path.Clear(); RenderCurrentLevel(); }
    private IReadOnlyList<InventoryCategoryNodeDto> CurrentChildren => _path.Count == 0 ? _root : _path[^1].Children;

    private void RenderCurrentLevel()
    {
        RenderBreadcrumb(); _list.SuspendLayout(); _list.Controls.Clear();
        var children = CurrentChildren.Where(n => string.IsNullOrWhiteSpace(_search.Text) || n.Name.Contains(_search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)).OrderBy(n => n.Name).ToArray();
        foreach (var node in children) _list.Controls.Add(BuildCategoryRow(node));
        if (_path.Count > 0 && CurrentChildren.Count == 0)
        {
            var items = _path[^1].Items ?? [];
            foreach (var item in items.Where(i => string.IsNullOrWhiteSpace(_search.Text) || i.Name.Contains(_search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                _list.Controls.Add(BuildItemRow(item));
        }
        if (children.Length == 0 && (_path.Count == 0 || CurrentChildren.Count > 0))
            _list.Controls.Add(new Label { Text = "لا توجد فئات أو أصناف هنا.", AutoSize = true, Padding = new Padding(12), ForeColor = BrandPalette.MutedText });
        _list.ResumeLayout();
    }

    private Control BuildCategoryRow(InventoryCategoryNodeDto node)
    {
        var row = new Panel { Width = Math.Max(360, ClientSize.Width - 36), Height = 46, Margin = new Padding(3), BackColor = BrandPalette.SoftGreen, Cursor = Cursors.Hand };
        var label = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(12, 0, 12, 0), Text = $"📁  {node.Name}    {(node.Children.Count > 0 ? "‹" : "")}", Font = Font, Cursor = Cursors.Hand };
        row.Controls.Add(label); void Drill(object? _, EventArgs __) { _path.Add(node); _search.Clear(); RenderCurrentLevel(); }
        row.Click += Drill; label.Click += Drill; return row;
    }

    private Control BuildItemRow(InventoryItemDto item)
    {
        var row = new TableLayoutPanel { Width = Math.Max(700, ClientSize.Width - 36), Height = 76, ColumnCount = 6, RowCount = 1, Margin = new Padding(3, 5, 3, 5), Padding = new Padding(8, 6, 8, 6), BackColor = Color.White };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
        row.Controls.Add(new Label { Text = $"{item.Name}  (المتاح: {item.Quantity})", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = true }, 0, 0);
        row.Controls.Add(new Label { Text = "سعر الفاتورة", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 1, 0);
        var price = new NumericUpDown { Minimum = 0, Maximum = 10_000_000, DecimalPlaces = 2, ThousandsSeparator = true, Value = Math.Clamp(item.UnitPrice, 0m, 10_000_000m), Dock = DockStyle.Fill, TextAlign = HorizontalAlignment.Center, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        price.BackColor = BrandPalette.SoftBlue; price.AccessibleName = $"سعر بيع {item.Name}"; row.Controls.Add(price, 2, 0);
        row.Controls.Add(new Label { Text = "الكمية المضافة", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false }, 3, 0);
        var quantity = new NumericUpDown { Minimum = 1, Maximum = Math.Max(1, Math.Min(9999, item.Quantity)), Value = 1, Dock = DockStyle.Fill, TextAlign = HorizontalAlignment.Center, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        quantity.AccessibleName = $"الكمية المضافة من {item.Name}"; row.Controls.Add(quantity, 4, 0);
        var add = BrandControls.ActionButton("إضافة", true); add.Dock = DockStyle.Fill; add.Height = 38;
        add.Click += (_, _) => { PickedUnitPrice = price.Value; PickedQuantity = (int)quantity.Value; ItemPicked?.Invoke(item); }; row.Controls.Add(add, 5, 0);
        return row;
    }

    private void RenderBreadcrumb()
    {
        _breadcrumb.Controls.Clear(); _breadcrumb.Controls.Add(Crumb("كل الفئات", () => { _path.Clear(); RenderCurrentLevel(); }));
        for (var i = 0; i < _path.Count; i++)
        {
            var captured = i; _breadcrumb.Controls.Add(new Label { Text = "›", AutoSize = true, Padding = new Padding(5, 8, 5, 0) });
            _breadcrumb.Controls.Add(Crumb(_path[i].Name, () => { _path.RemoveRange(captured + 1, _path.Count - captured - 1); RenderCurrentLevel(); }));
        }
    }
    private static Control Crumb(string text, Action action)
    {
        var button = BrandControls.ActionButton(text, autoSize: true); button.Height = 32; button.MinimumSize = new Size(0, 32); button.Click += (_, _) => action(); return button;
    }
}
