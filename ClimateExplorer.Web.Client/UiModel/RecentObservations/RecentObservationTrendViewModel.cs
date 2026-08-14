namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

using ClimateExplorer.Core.Stats.Model;

public sealed record RecentObservationTrendViewModel
{
    public string Label { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public int CompleteYearCount { get; init; }
    public int MinimumRequiredYears { get; init; }
    public string? HeadlineText { get; init; }
    public bool IsHeadlinePositive { get; init; }
    public string? HeadlineCaption { get; init; }
    public string? FullPeriodTooltip { get; init; }
    public string? RecentTrendYearRange { get; init; }
    public string? RecentTrendValueText { get; init; }
    public bool IsRecentTrendPositive { get; init; }
    public string? RecentTrendTooltip { get; init; }
    public string? FirstHalfTrendYearRange { get; init; }
    public string? FirstHalfTrendValueText { get; init; }
    public bool IsFirstHalfTrendPositive { get; init; }
    public string? FirstHalfTrendTooltip { get; init; }
    public string? UnavailableReason { get; init; }

    // The full PolynomialRegressionResult and the exact point list each was calculated from - carried
    // through so the About-trends modal can render the full statistical breakdown and the download
    // button can export data that reproduces the regression, without recalculating anything.
    public PolynomialRegressionResult? FullPeriodTrend { get; init; }
    public PolynomialRegressionResult? RecentTrend { get; init; }
    public PolynomialRegressionResult? FirstHalfTrend { get; init; }
    public IReadOnlyList<DataPoint> FullPeriodPoints { get; init; } = [];
    public IReadOnlyList<DataPoint> RecentTrendPoints { get; init; } = [];
    public IReadOnlyList<DataPoint> FirstHalfTrendPoints { get; init; } = [];
}
