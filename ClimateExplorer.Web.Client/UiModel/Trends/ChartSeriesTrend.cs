namespace ClimateExplorer.Web.Client.UiModel.Trends;

using ClimateExplorer.Core.Stats.Model;

/// <summary>
/// Everything the trend module knows about one chart series, computed in a single pass: all three
/// windows fitted, which of them are significant enough to offer in the dropdown, and the forward
/// projection for the one currently selected.
/// </summary>
/// <remarks>
/// This is derived state - rebuilt on every chart build and never serialised. The user's intent
/// (whether the module is on, which window, how many years) lives on the chart series definition.
/// </remarks>
public sealed record ChartSeriesTrend
{
    public required TrendStatSubject Subject { get; init; }

    /// <summary>All three windows, in Full/Recent/FirstHalf order. Empty when the series had too few years.</summary>
    public IReadOnlyList<ChartSeriesTrendWindowResult> Windows { get; init; } = [];

    /// <summary>Every plotted point in the series, whichever window is selected.</summary>
    public IReadOnlyList<DataPoint> Points { get; init; } = [];

    /// <summary>Set when no window could be fitted at all - typically too few years of data.</summary>
    public string? UnavailableReason { get; init; }

    /// <summary>The projection to draw, or null when nothing significant is available to draw.</summary>
    public ChartSeriesTrendProjection? Projection { get; init; }

    /// <summary>The last year of real data - the projection starts the year after this.</summary>
    public int LastDataYear { get; init; }

    public IReadOnlyList<TrendWindow> SignificantWindows => [.. Windows.Where(x => x.IsSignificant).Select(x => x.Window)];

    public bool HasSignificantWindow => Windows.Any(x => x.IsSignificant);

    public ChartSeriesTrendWindowResult? GetWindow(TrendWindow window) => Windows.FirstOrDefault(x => x.Window == window);
}
