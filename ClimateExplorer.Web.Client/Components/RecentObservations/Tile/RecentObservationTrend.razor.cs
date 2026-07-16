namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationTrend
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<RecentObservationTrendViewModel> Metrics { get; set; } = [];

    private static string? GetTrendValueClass(string? valueText, bool isPositive)
    {
        if (isPositive)
        {
            return "positive-trend";
        }

        return valueText is not null && valueText.StartsWith('-') ? "negative-trend" : null;
    }
}
