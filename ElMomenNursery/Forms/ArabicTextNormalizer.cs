using ElMomenNursery.Application.Common;

namespace ElMomenNursery.Forms;

internal static class ArabicTextNormalizer
{
    public static string Normalize(string? value)
    {
        return ArabicNameNormalizer.Normalize(value);
    }
}
