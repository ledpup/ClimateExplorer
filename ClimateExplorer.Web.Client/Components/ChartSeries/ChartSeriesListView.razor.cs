namespace ClimateExplorer.Web.Client.Components.ChartSeries;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

public partial class ChartSeriesListView
{
    [Parameter]
    public List<ChartSeriesDefinition>? ChartSeriesList { get; set; }

    [Parameter]
    public IReadOnlyList<SeriesWithData>? SeriesWithData { get; set; }

    [Parameter]
    public EventCallback OnSeriesChanged { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    private List<ChartSeriesDefinition>? ChartSeriesListInternal { get; set; }

    protected override void OnParametersSet()
    {
        ChartSeriesListInternal = ChartSeriesList?.Where(x => x.DataAvailable).ToList();
    }

    private async Task OnSeriesChangedInternal()
    {
        await OnSeriesChanged.InvokeAsync();
    }

    private IReadOnlyList<DataSetSourceMetadata>? GetSourceMetadata(ChartSeriesDefinition chartSeries)
    {
        return SeriesWithData?
            .FirstOrDefault(x => x.ChartSeries.Id == chartSeries.Id)
            ?.SourceDataSet
            .SourceMetadata;
    }

    private async Task OnRemoveSeries(ChartSeriesDefinition csd)
    {
        ChartSeriesList!.Remove(csd);

        await OnSeriesChangedInternal();
    }

    private async Task OnDuplicateSeries(ChartSeriesDefinition csd)
    {
        ChartSeriesList!.Add(
            new ChartSeriesDefinition()
            {
                Aggregation = csd.Aggregation,
                BinGranularity = csd.BinGranularity,
                SeriesDerivationType = csd.SeriesDerivationType,
                SourceSeriesSpecifications =
                    csd.SourceSeriesSpecifications!
                    .Select(
                        x =>
                        new SourceSeriesSpecification
                        {
                            DataSetDefinition = x.DataSetDefinition,
                            LocationId = x.LocationId,
                            LocationName = x.LocationName,
                            MeasurementDefinition = x.MeasurementDefinition,
                        })
                    .ToArray(),
                Smoothing = csd.Smoothing,
                SmoothingWindow = csd.SmoothingWindow,
                Value = csd.Value,
                Year = csd.Year,
                SeriesTransformation = csd.SeriesTransformation,
                GroupingThreshold = csd.GroupingThreshold,
                MinimumDataResolution = csd.MinimumDataResolution,
            });

        await OnSeriesChangedInternal();
    }
}
