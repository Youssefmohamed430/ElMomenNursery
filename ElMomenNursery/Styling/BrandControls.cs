using System.Drawing;

namespace ElMomenNursery.Styling;

/// <summary>
/// Shared factory for the application's flat action buttons. Keeping one implementation
/// guarantees that every screen (dialogs, toolbars, filter cards) uses the same button look.
/// </summary>
internal static class BrandControls
{
    internal static Button ActionButton(string text, bool primary = false, bool autoSize = false)
    {
        var button = new Button
        {
            Text = text,
            Height = 42,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? BrandPalette.PrimaryGreen : BrandPalette.Surface,
            ForeColor = primary ? Color.White : BrandPalette.Text,
            Font = new Font("Segoe UI", 9.5F, primary ? FontStyle.Bold : FontStyle.Regular),
            Cursor = Cursors.Hand,
            RightToLeft = RightToLeft.Yes,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = primary ? BrandPalette.PrimaryGreen : BrandPalette.Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = primary ? BrandPalette.PrimaryGreenHover : BrandPalette.SoftGreen;
        if (autoSize)
        {
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowOnly;
            button.MinimumSize = new Size(0, 42);
            button.Padding = new Padding(14, 0, 14, 0);
        }
        return button;
    }
}
