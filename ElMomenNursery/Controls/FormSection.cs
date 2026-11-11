using ElMomenNursery.Styling;

namespace ElMomenNursery.Controls;

public sealed class FormSection : Panel
{
    public FormSection(string title)
    {
        Height = 190;
        Padding = new Padding(18, 12, 18, 16);
        BackColor = BrandPalette.Surface;
        BorderStyle = BorderStyle.FixedSingle;
        RightToLeft = RightToLeft.Yes;

        var heading = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 38,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = BrandPalette.PrimaryGreen,
            RightToLeft = RightToLeft.Yes
        };

        Content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            RightToLeft = RightToLeft.Yes,
            Padding = new Padding(0, 8, 0, 0)
        };

        Controls.Add(Content);
        Controls.Add(heading);
    }

    public Panel Content { get; }
}

