using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

public sealed class LoadingIndicator : UserControl
{
    public LoadingIndicator(string message = "جارٍ التحميل، برجاء الانتظار")
    {
        Height = 54;
        BackColor = BrandPalette.Surface;
        RightToLeft = RightToLeft.Yes;
        Padding = new Padding(14, 8, 14, 8);
        AccessibleRole = AccessibleRole.ProgressBar;

        var progress = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 28,
            Width = 150,
            Height = 18,
            Dock = DockStyle.Left,
            AccessibleName = "جارٍ التحميل"
        };

        var messageLabel = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = BrandPalette.Text,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            RightToLeft = RightToLeft.Yes
        };

        Controls.Add(messageLabel);
        Controls.Add(progress);
    }
}

