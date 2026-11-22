using System.Drawing;

namespace ElMomenNursery.Styling;

internal static class BrandPalette
{
    // The bright green and red are sampled from the supplied nursery logo.
    internal static readonly Color LogoGreen = Color.FromArgb(128, 176, 64);
    internal static readonly Color LogoRed = Color.FromArgb(224, 0, 0);
    internal static readonly Color LogoBlue = Color.FromArgb(0, 64, 224);

    // Darker green is used for text and primary actions to keep white text readable.
    internal static readonly Color PrimaryGreen = Color.FromArgb(55, 105, 39);
    internal static readonly Color PrimaryGreenHover = Color.FromArgb(43, 86, 30);
    internal static readonly Color SoftGreen = Color.FromArgb(235, 244, 226);
    internal static readonly Color SoftRed = Color.FromArgb(255, 239, 237);
    internal static readonly Color SoftBlue = Color.FromArgb(236, 243, 255);
    internal static readonly Color AppBackground = Color.FromArgb(247, 249, 245);
    internal static readonly Color Surface = Color.White;
    internal static readonly Color Border = Color.FromArgb(222, 230, 218);
    internal static readonly Color FilterBorderStrong = Color.FromArgb(195, 211, 186);
    internal static readonly Color Text = Color.FromArgb(35, 48, 34);
    internal static readonly Color MutedText = Color.FromArgb(100, 113, 99);
    internal static readonly Color DangerText = Color.FromArgb(165, 24, 24);
    internal static readonly Color InfoText = Color.FromArgb(29, 69, 142);
}

