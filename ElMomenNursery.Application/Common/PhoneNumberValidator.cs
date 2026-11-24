using System.Text.RegularExpressions;

namespace ElMomenNursery.Application.Common;

public static partial class PhoneNumberValidator
{
    public static bool IsValidEgyptianMobile(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var digits = new string(value.Trim().Select(c => char.GetNumericValue(c) is >= 0 and <= 9
            ? (char)('0' + (int)char.GetNumericValue(c)) : '\0').Where(char.IsDigit).ToArray());

        if (digits.StartsWith("0020", StringComparison.Ordinal)) digits = digits[4..];
        else if (digits.StartsWith("20", StringComparison.Ordinal) && digits.Length == 12) digits = digits[2..];
        return MobileRegex().IsMatch(digits);
    }

    [GeneratedRegex("^01[0125][0-9]{8}$", RegexOptions.CultureInvariant)]
    private static partial Regex MobileRegex();
}
