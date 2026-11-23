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

    public ActionToolbar()
    {
        BackColor = BrandPalette.AppBackground;
        Margin = Padding.Empty;
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
            Controls.Add(control);
        }
    }

    /// <summary>Arranges the toolbar for the given width, wraps rows, sets its height, and returns it.</summary>
    public int ArrangeItems(int width)
    {
        var x = width;
        var y = 0;
        var placedInRow = false;
        foreach (var item in _items)
        {
            if (!item.Visible) continue;
            var itemWidth = item.Width > 0 ? item.Width : item.PreferredSize.Width;
            if (placedInRow && x - itemWidth < 0)
            {
                y += ItemHeight + RowGap;
                x = width;
            }
            x -= itemWidth;
            item.SetBounds(x, y + (ItemHeight - item.Height) / 2, itemWidth, item.Height);
            x -= ItemSpacing;
            placedInRow = true;
        }

        var height = _items.Any(item => item.Visible) ? y + ItemHeight : 0;
        if (Height != height)
        {
            Height = height;
            LayoutChanged?.Invoke(this, EventArgs.Empty);
        }
        return height;
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible && Width > 0) ArrangeItems(Width);
    }
}
