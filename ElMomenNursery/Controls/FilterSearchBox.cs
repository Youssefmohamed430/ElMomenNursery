using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

/// <summary>
/// Shared filter search input: a bordered white field with a painted magnifier glyph on the
/// right (RTL) side. The inner <see cref="Input"/> keeps normal <see cref="TextBox"/> behaviour,
/// so pages bind to it exactly like a plain text box.
/// </summary>
public sealed class FilterSearchBox : Panel
{
    private readonly Panel _icon;

    public FilterSearchBox(string placeholder)
    {
        Height = FilterTheme.InputHeight;
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = BrandPalette.Surface;
        Margin = Padding.Empty;

        Input = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = BrandPalette.Surface,
            Font = FilterTheme.CreateInputFont(),
            RightToLeft = RightToLeft.Yes,
            PlaceholderText = placeholder
        };
        _icon = new Panel { BackColor = BrandPalette.Surface, Width = 30, Height = FilterTheme.InputHeight };
        _icon.Paint += (_, eventArgs) => PaintMagnifier(eventArgs.Graphics, _icon.ClientSize);

        Controls.Add(_icon);
        Controls.Add(Input);
        Resize += (_, _) => ArrangeChildren();
        ArrangeChildren();
    }

    public TextBox Input { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new string Text
    {
        get => Input.Text;
        set => Input.Text = value;
    }

    public void Clear() => Input.Clear();

    private void ArrangeChildren()
    {
        _icon.SetBounds(Math.Max(0, ClientSize.Width - _icon.Width), 0, _icon.Width, ClientSize.Height);
        var textHeight = Math.Min(Input.PreferredHeight, ClientSize.Height);
        Input.SetBounds(4, Math.Max(0, (ClientSize.Height - textHeight) / 2),
            Math.Max(40, ClientSize.Width - _icon.Width - 8), textHeight);
    }

    private static void PaintMagnifier(Graphics graphics, Size area)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(BrandPalette.MutedText, 1.6F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        const int diameter = 11;
        var x = (area.Width - diameter) / 2 - 1;
        var y = (area.Height - diameter) / 2 - 1;
        graphics.DrawEllipse(pen, x, y, diameter, diameter);
        graphics.DrawLine(pen, x + diameter - 1, y + diameter - 1, x + diameter + 3, y + diameter + 3);
    }
}
