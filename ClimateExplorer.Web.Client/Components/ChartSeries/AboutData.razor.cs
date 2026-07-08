namespace ClimateExplorer.Web.Client.Components.ChartSeries;

using Blazorise;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

public partial class AboutData
{
    private Modal? modal;

    [Parameter]
    public ChartSeriesDefinition? ChartSeries { get; set; }

    [Parameter]
    public IReadOnlyList<DataSetMetadata>? SourceMetadata { get; set; }

    public Task Show()
    {
        return modal!.Show();
    }

    private DataSetMetadata? FindSourceMetadata(SourceSeriesSpecification sourceSeriesSpecification)
    {
        return SourceMetadata?.FirstOrDefault(
            x => x.DataSetDefinitionId == sourceSeriesSpecification.DataSetDefinition?.Id &&
                x.LocationId == sourceSeriesSpecification.LocationId);
    }
}
