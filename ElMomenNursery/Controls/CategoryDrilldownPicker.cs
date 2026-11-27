using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

internal sealed class CategoryDrilldownPicker : UserControl
{
    public event Action<InventoryItemDto>? ItemPicked;
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
        var row = new TableLayoutPanel { Width = Math.Max(400, ClientSize.Width - 36), Height = 48, ColumnCount = 3, RowCount = 1, Margin = new Padding(3), Padding = new Padding(5), BackColor = Color.White };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        row.Controls.Add(new Label { Text = item.Name, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 0);
        row.Controls.Add(new Label { Text = $"{item.UnitPrice:N2} ج.م  (المتاح: {item.Quantity})", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 1, 0);
        var add = BrandControls.ActionButton("إضافة", true); add.Dock = DockStyle.Fill; add.Height = 34; add.Click += (_, _) => ItemPicked?.Invoke(item); row.Controls.Add(add, 2, 0);
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
