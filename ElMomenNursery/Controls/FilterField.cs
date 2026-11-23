using System.ComponentModel;
using System.Drawing;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

/// <summary>
/// One filter control with its label above it. The block is the unit laid out by
/// <see cref="FilterCard"/>; the wrapped input keeps its original identity, events, and bindings.
/// </summary>
public sealed class FilterField : Panel
{
    public FilterField(string name, string labelText, Control input)
    {
        Name = name;
        Height = FilterTheme.FieldHeight;
        BackColor = BrandPalette.Surface;
        Margin = Padding.Empty;
        RightToLeft = RightToLeft.Yes;

        Label = FilterTheme.CreateLabel(labelText);
        Input = input;
        Controls.Add(Input);
        Controls.Add(Label);
        ResizeTo(Width);
    }

    public Label Label { get; }

    public Control Input { get; }

    /// <summary>Single-unit filters that need a wider cell (search boxes, paired ranges).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ColumnSpan { get; set; } = 1;

    public void ResizeTo(int width)
    {
        Width = width;
        Label.Width = width;
        Input.Width = width;
        var inputHeight = MeasureInputHeight(Input);
        if (Input.Height != inputHeight) Input.Height = inputHeight;
        Input.Top = FilterTheme.InputTop + (FilterTheme.InputHeight - inputHeight) / 2;
    }

    private static int MeasureInputHeight(Control input)
    {
        if (input is FilterSearchBox) return FilterTheme.InputHeight;
        var preferred = input.PreferredSize.Height;
        if (preferred <= 0) return FilterTheme.InputHeight;
        return Math.Min(FilterTheme.InputHeight, preferred);
    }
}
