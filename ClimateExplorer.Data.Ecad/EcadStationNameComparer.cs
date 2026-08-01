namespace ClimateExplorer.Data.Ecad;

using System.Globalization;
using System.Text;

/// <summary>
/// Compares GHCN and ECA&amp;D station names, which describe the same sites in incompatible styles:
/// GHCN uses upper case with underscores and a disambiguating numeric suffix ("DE_BILT_1"), while
/// ECA&amp;D uses ordinary mixed case with diacritics and parenthesised qualifiers ("De Bilt").
/// </summary>
public static class EcadStationNameComparer
{
    /// <summary>
    /// Score from 0 to 1. Identical normalised names score 1; one name containing the other scores 0.9,
    /// which covers the common case of a site qualifier only one publisher records ("WIEN" against
    /// "Wien-Hohe Warte"); anything else falls back to the proportion of characters the two share.
    /// </summary>
    public static double GetSimilarity(string? first, string? second)
    {
        var left = Normalise(first);
        var right = Normalise(second);
        if (left.Length == 0 || right.Length == 0)
        {
            return 0d;
        }

        if (string.Equals(left, right, StringComparison.Ordinal))
        {
            return 1d;
        }

        if (left.Contains(right, StringComparison.Ordinal) || right.Contains(left, StringComparison.Ordinal))
        {
            return 0.9d;
        }

        return GetCommonSubsequenceRatio(left, right);
    }

    public static bool IsSameName(string? first, string? second)
    {
        var left = Normalise(first);
        return left.Length > 0 && string.Equals(left, Normalise(second), StringComparison.Ordinal);
    }

    /// <summary>
    /// Strips diacritics, case, punctuation and whitespace, then drops a trailing number - GHCN appends
    /// one to distinguish successive sites at the same place ("DE_BILT_1"), and ECA&amp;D does not.
    /// </summary>
    public static string Normalise(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var decomposed = name.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var lastWasSeparator = false;
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
                lastWasSeparator = false;
            }
            else if (builder.Length > 0 && !lastWasSeparator)
            {
                builder.Append(' ');
                lastWasSeparator = true;
            }
        }

        var normalised = builder.ToString().Trim();
        var lastSpace = normalised.LastIndexOf(' ');
        if (lastSpace > 0 && normalised.AsSpan(lastSpace + 1).ToString().All(char.IsAsciiDigit))
        {
            normalised = normalised[..lastSpace];
        }

        return normalised.Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// Twice the length of the longest common subsequence over the combined length - the same ratio
    /// Python's difflib and most "how alike are these strings" measures report, bounded to 0..1.
    /// </summary>
    private static double GetCommonSubsequenceRatio(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var i = 1; i <= left.Length; i++)
        {
            for (var j = 1; j <= right.Length; j++)
            {
                current[j] = left[i - 1] == right[j - 1]
                    ? previous[j - 1] + 1
                    : Math.Max(previous[j], current[j - 1]);
            }

            (previous, current) = (current, previous);
            Array.Clear(current);
        }

        return 2d * previous[right.Length] / (left.Length + right.Length);
    }
}
