namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using System.Globalization;

internal static class ValueClassifier
{
    public static string? GetValueClass(string? valueText, bool isPositive)
    {
        // A value that rounds to zero (e.g. "+0.0°C" or "-0.0×") reads as "no
        // change" and must not be tinted red/blue as if it were a real signal.
        if (IsZeroMagnitude(valueText))
        {
            return null;
        }

        if (isPositive)
        {
            return "positive-value";
        }

        return valueText is not null && valueText.StartsWith('-') ? "negative-value" : null;
    }

    private static bool IsZeroMagnitude(string? valueText)
    {
        if (string.IsNullOrEmpty(valueText))
        {
            return false;
        }

        var start = valueText[0] is '+' or '-' ? 1 : 0;
        var end = start;
        while (end < valueText.Length && (char.IsAsciiDigit(valueText[end]) || valueText[end] == '.'))
        {
            end++;
        }

        return end > start &&
            double.TryParse(valueText.AsSpan(start, end - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var magnitude) &&
            magnitude == 0d;
    }
}
