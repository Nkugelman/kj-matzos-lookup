namespace KjMatzosLookup.Domain.Calendar;

/// <summary>
/// Approximate Gregorian → Hebrew civil-year mapping used to group purchases by year.
/// Pure domain logic with no infrastructure dependencies.
/// </summary>
public static class HebrewYearHelper
{
    private static readonly Dictionary<int, string> YearOnes = new()
    {
        [0] = "י", [1] = "א", [2] = "ב", [3] = "ג", [4] = "ד",
        [5] = "ה", [6] = "ו", [7] = "ז", [8] = "ח", [9] = "ט",
    };

    public static int FromDate(DateTime date)
    {
        var y = date.Year;
        var m = date.Month;
        var d = date.Day;
        var anchor = m < 9 || (m == 9 && d < 10) ? y - 1 : y;
        return anchor + 3760;
    }

    public static string FormatLabel(int hebrewYear)
    {
        var ones = hebrewYear % 10;
        return YearOnes.TryGetValue(ones, out var letter)
            ? $"תשפ״{letter}"
            : $"תשפ״";
    }
}
