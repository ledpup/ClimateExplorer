namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.UiModel;

/// <summary>
/// The output of <see cref="IChartDataBuilder"/>: the fetched and processed series data,
/// plus the bin range, start years, any user-facing warnings, and whether the result
/// contains anything renderable. <see cref="ChartView"/> consumes this to render the chart
/// without owning data fetching or preparation.
/// </summary>
public sealed record ChartDataBuildResult
{
    public IReadOnlyList<SeriesWithData> SeriesWithData { get; init; } = [];

    public IReadOnlyList<SeriesWithData> NonRenderedSeriesWithData { get; init; } = [];

    public BinIdentifier[]? ChartBins { get; init; }

    public BinIdentifier? ChartStartBin { get; init; }

    public BinIdentifier? ChartEndBin { get; init; }

    public IReadOnlyList<short> StartYears { get; init; } = [];

    public IReadOnlyList<SnackbarMessage> Messages { get; init; } = [];

    public bool HasRenderableData { get; init; }
}
