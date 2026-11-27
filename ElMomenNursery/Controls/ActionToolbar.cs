using System.ComponentModel;
using System.Drawing;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

/// <summary>
/// A right-to-left wrapping toolbar for a screen's operational buttons (add, edit, delete,
/// export, record attendance...). It keeps those actions visually separate from the filter
/// card and reports the height it needs so pages can lay out the rest of the content.
/// </summary>
public sealed class ActionToolbar : Panel
{
    private readonly List<Control> _items = [];
    private readonly Panel _viewport = new() { BackColor = Color.Transparent };
    private readonly HScrollBar _horizontalScroll = new() { Visible = false, RightToLeft = RightToLeft.Yes };
    private int _contentWidth;
    private int _scrollValue;

    public ActionToolbar()
    {
        BackColor = BrandPalette.AppBackground;
        Margin = Padding.Empty;
        Controls.Add(_viewport);
        Controls.Add(_horizontalScroll);
        _horizontalScroll.Scroll += (_, _) => { _scrollValue = _horizontalScroll.Value; PositionItems(); };
    }

    public IReadOnlyList<Control> Items => _items;

    public event EventHandler? LayoutChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ItemSpacing { get; set; } = 8;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int RowGap { get; set; } = 6;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ItemHeight { get; set; } = FilterTheme.ToolbarHeight;

    public void Add(params Control[] controls)
    {
        foreach (var control in controls)
        {
            if (control is Button button) button.Height = ItemHeight;
            _items.Add(control);
            _viewport.Controls.Add(control);
        }
    }

    /// <summary>Arranges all actions in one right-to-left row and enables horizontal scrolling when needed.</summary>
    public int ArrangeItems(int width)
    {
        var visible = _items.Where(item => item.Visible).ToArray();
        var widths = visible.Select(item => item.Width > 0 ? Math.Max(item.Width, item.PreferredSize.Width) : item.PreferredSize.Width).ToArray();
        _contentWidth = widths.Sum() + Math.Max(0, visible.Length - 1) * ItemSpacing;
        var hasOverflow = _contentWidth > width;
        _horizontalScroll.Visible = hasOverflow;
        var scrollHeight = hasOverflow ? SystemInformation.HorizontalScrollBarHeight : 0;
        _viewport.SetBounds(0, 0, width, ItemHeight);
        _horizontalScroll.SetBounds(0, ItemHeight, width, scrollHeight);
        var maxScroll = Math.Max(0, _contentWidth - width);
        _scrollValue = hasOverflow ? maxScroll : 0;
        if (hasOverflow)
        {
            _horizontalScroll.Minimum = 0;
            _horizontalScroll.LargeChange = Math.Max(1, width / 2);
            _horizontalScroll.SmallChange = Math.Max(1, ItemSpacing * 2);
            _horizontalScroll.Maximum = maxScroll + _horizontalScroll.LargeChange - 1;
            _horizontalScroll.Value = _scrollValue;
        }
        PositionItems(visible, widths);

        var height = visible.Length > 0 ? ItemHeight + scrollHeight : 0;
        if (Height != height)
        {
            Height = height;
            LayoutChanged?.Invoke(this, EventArgs.Empty);
        }
        return height;
    }

    private void PositionItems() => PositionItems(_items.Where(item => item.Visible).ToArray(),
        _items.Where(item => item.Visible).Select(item => item.Width > 0 ? Math.Max(item.Width, item.PreferredSize.Width) : item.PreferredSize.Width).ToArray());

    private void PositionItems(IReadOnlyList<Control> visible, IReadOnlyList<int> widths)
    {
        var x = Math.Max(_contentWidth, ClientSize.Width) - _scrollValue;
        for (var i = 0; i < visible.Count; i++)
        {
            x -= widths[i];
            visible[i].SetBounds(x, (ItemHeight - visible[i].Height) / 2, widths[i], visible[i].Height);
            x -= ItemSpacing;
        }
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible && Width > 0) ArrangeItems(Width);
    }
}
