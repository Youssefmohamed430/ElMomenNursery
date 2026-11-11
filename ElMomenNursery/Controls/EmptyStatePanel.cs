using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

public sealed class EmptyStatePanel : Panel
{
    public EmptyStatePanel(string heading, string message)
    {
        BackColor = BrandPalette.Surface;
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(28, 24, 28, 24);
        RightToLeft = RightToLeft.Yes;
        AccessibleRole = AccessibleRole.Grouping;

        var headingLabel = new Label
        {
            Text = heading,
            Dock = DockStyle.Top,
            Height = 38,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = BrandPalette.Text,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point),
            RightToLeft = RightToLeft.Yes
        };

        var messageLabel = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopCenter,
            ForeColor = BrandPalette.MutedText,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            RightToLeft = RightToLeft.Yes
        };

        Controls.Add(messageLabel);
        Controls.Add(headingLabel);
    }
}
