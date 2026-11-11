using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

public enum MessageTone
{
    Information,
    Success,
    Warning,
    Error
}

public sealed class FeedbackBanner : Panel
{
    public FeedbackBanner(string heading, string message, MessageTone tone = MessageTone.Information)
    {
        var (background, foreground) = tone switch
        {
            MessageTone.Success => (BrandPalette.SoftGreen, BrandPalette.PrimaryGreen),
            MessageTone.Warning => (Color.FromArgb(255, 248, 225), Color.FromArgb(116, 78, 0)),
            MessageTone.Error => (BrandPalette.SoftRed, BrandPalette.DangerText),
            _ => (BrandPalette.SoftBlue, BrandPalette.InfoText)
        };

        Height = 76;
        BackColor = background;
        Padding = new Padding(16, 10, 16, 10);
        RightToLeft = RightToLeft.Yes;
        BorderStyle = BorderStyle.FixedSingle;
        AccessibleRole = AccessibleRole.StatusBar;

        var headingLabel = new Label
        {
            Text = heading,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = foreground,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
            RightToLeft = RightToLeft.Yes
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
        Controls.Add(headingLabel);
    }
}

