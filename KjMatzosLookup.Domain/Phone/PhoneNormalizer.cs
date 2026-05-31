using System.Text.RegularExpressions;

namespace KjMatzosLookup.Domain.Phone;

/// <summary>
/// Pure phone-number domain logic: normalization, matching and display formatting.
/// No infrastructure dependencies.
/// </summary>
public static class PhoneNormalizer
{
    private static readonly Regex NonDigits = new(@"[^\d]", RegexOptions.Compiled);

    public static string DigitsOnly(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        return NonDigits.Replace(phone, string.Empty);
    }

    /// <summary>US-style match key: last 10 digits when long enough.</summary>
    public static string MatchKey(string? phone)
    {
        var digits = DigitsOnly(phone);
        if (digits.Length >= 10)
            return digits[^10..];
        return digits;
    }

    public static bool PhoneMatches(string? stored, string searchKey)
    {
        if (string.IsNullOrEmpty(searchKey)) return false;
        var storedKey = MatchKey(stored);
        if (string.IsNullOrEmpty(storedKey)) return false;
        return storedKey == searchKey;
    }

    public static string Format(string? phone)
    {
        var digits = DigitsOnly(phone);
        if (digits.Length >= 10)
            return $"({digits[^10..^7]}) {digits[^7..^4]}-{digits[^4..]}";
        if (digits.Length >= 7)
            return $"({digits[..3]}) {digits[3..6]}-{digits[6..]}";
        return digits;
    }

    [Obsolete("Kiosk shows full phone numbers per customer requirement.")]
    public static string Mask(string? phone)
    {
        var digits = DigitsOnly(phone);
        if (digits.Length < 4) return "(***) ***-****";
        return $"(***) ***-{digits[^4..]}";
    }
}
