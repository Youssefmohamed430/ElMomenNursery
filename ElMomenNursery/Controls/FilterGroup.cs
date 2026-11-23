using System.Drawing;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

/// <summary>
/// A labelled filter block that groups several small controls (for example quick date ranges)
/// under one label, so every filter still shows a label above its controls.
/// </summary>
public sealed class FilterGroup : Panel
{
    private readonly Control[] _items;
    private const int ItemGap = 8;

    public FilterGroup(string name, string labelText, params Control[] items)
    {
        Name = name;
        Height = FilterTheme.FieldHeight;
        BackColor = BrandPalette.Surface;
        Margin = Padding.Empty;
        RightToLeft = RightToLeft.Yes;

        Label = FilterTheme.CreateLabel(labelText);
        _items = items;
        Controls.Add(Label);
        foreach (var item in items) Controls.Add(item);
        ResizeTo(Width);
    }

    public Label Label { get; }

    public void ResizeTo(int width)
    {
        Width = width;
        Label.Width = width;
        if (_items.Length == 0) return;

        var itemWidth = Math.Max(40, (width - ItemGap * (_items.Length - 1)) / _items.Length);
        for (var index = 0; index < _items.Length; index++)
        {
            var item = _items[index];
            var height = Math.Min(FilterTheme.InputHeight, Math.Max(28, item.PreferredSize.Height));
            var x = width - itemWidth * (index + 1) - ItemGap * index;
            item.SetBounds(x, FilterTheme.InputTop + (FilterTheme.InputHeight - height) / 2, itemWidth, height);
        }
    }
}
