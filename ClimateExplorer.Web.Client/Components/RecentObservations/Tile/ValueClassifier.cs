namespace ClimateExplorer.Web.Client.Components.RecentObservations;

internal static class ValueClassifier
{
    public static string? GetValueClass(string? valueText, bool isPositive)
    {
        if (isPositive)
        {
            return "positive-value";
        }

        return valueText is not null && valueText.StartsWith('-') ? "negative-value" : null;
    }
}
