using System.Drawing;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

/// <summary>
/// The filter action pair (apply / clear / refresh). Each action takes one grid cell so the
/// action row keeps the same rhythm as the filter fields above it.
/// </summary>
public sealed class FilterActionGroup : Panel
{
    private readonly Button[] _buttons;

    public FilterActionGroup(params Button[] buttons)
    {
        Name = "filter.actions";
        Height = FilterTheme.FieldHeight;
        BackColor = BrandPalette.Surface;
        Margin = Padding.Empty;
        RightToLeft = RightToLeft.Yes;
        _buttons = buttons;
        foreach (var button in buttons)
        {
            button.Height = FilterTheme.ButtonHeight;
            Controls.Add(button);
        }
        ResizeTo(Width);
    }

    public IReadOnlyList<Button> Buttons => _buttons;

    public void ResizeTo(int width)
    {
        Width = width;
        if (_buttons.Length == 0) return;

        var buttonWidth = Math.Max(FilterTheme.MinButtonWidth,
            (width - FilterTheme.ButtonGap * (_buttons.Length - 1)) / _buttons.Length);
        var totalWidth = buttonWidth * _buttons.Length + FilterTheme.ButtonGap * (_buttons.Length - 1);
        var left = Math.Max(0, width - totalWidth);
        var top = FilterTheme.InputTop + (FilterTheme.InputHeight - FilterTheme.ButtonHeight) / 2;
        for (var index = 0; index < _buttons.Length; index++)
        {
            _buttons[index].SetBounds(left, top, buttonWidth, FilterTheme.ButtonHeight);
            left += buttonWidth + FilterTheme.ButtonGap;
        }
    }
}
