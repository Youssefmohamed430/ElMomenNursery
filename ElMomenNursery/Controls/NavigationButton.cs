using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

public sealed class NavigationButton : Button
{
    private bool _selected;

    public NavigationButton(string label)
    {
        Text = label;
        Height = 50;
        Margin = new Padding(0, 0, 0, 8);
        Padding = new Padding(14, 0, 14, 0);
        TextAlign = ContentAlignment.MiddleLeft;
        RightToLeft = RightToLeft.Yes;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = BrandPalette.SoftGreen;
        BackColor = Color.Transparent;
        ForeColor = BrandPalette.Text;
        Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = label;
        UseVisualStyleBackColor = false;
    }

    internal void SetSelected(bool selected)
    {
        _selected = selected;
        BackColor = selected ? BrandPalette.SoftGreen : Color.Transparent;
        ForeColor = selected ? BrandPalette.PrimaryGreen : BrandPalette.Text;
        Font = new Font("Segoe UI", 10.5F, selected ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
        Invalidate();
    }
}



