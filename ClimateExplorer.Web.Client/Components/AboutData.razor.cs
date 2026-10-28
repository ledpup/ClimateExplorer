namespace ClimateExplorer.Web.Client.Components;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

public partial class AboutData
{
    private Modal? modal;

    [Parameter]
    public ChartSeriesDefinition? ChartSeries { get; set; }

    [Parameter]
    public IReadOnlyList<DataSetMetadata>? SourceMetadata { get; set; }

    private string ModalTitle =>
        ChartSeries?.SourceSeriesSpecifications is { Length: 1 } specifications
            ? ChartSeriesDefinition.BuildFriendlyTitleShortForSeries(specifications[0], ChartSeries.BinGranularity, ChartSeries.Aggregation)
            : "About this data";

    public Task Show()
    {
        return modal!.ShowAsync();
    }

    private DataSetMetadata? FindSourceMetadata(SourceSeriesSpecification sourceSeriesSpecification)
    {
        return SourceMetadata?.FirstOrDefault(
            x => x.DataSetDefinitionId == sourceSeriesSpecification.DataSetDefinition?.Id &&
                x.LocationId == sourceSeriesSpecification.LocationId);
    }
}
