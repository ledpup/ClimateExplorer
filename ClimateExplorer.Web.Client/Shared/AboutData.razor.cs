namespace ClimateExplorer.Web.Client.Shared;

using Blazorise;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

public partial class AboutData
{
    private Modal? modal;

    [Parameter]
    public ChartSeriesDefinition? ChartSeries { get; set; }

    public Task Show()
    {
        return modal!.Show();
    }
}
