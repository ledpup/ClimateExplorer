namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;

public sealed record LocationDefaultChartContext
{
    public required Location Location { get; init; }

    public required IReadOnlyList<DataSetDefinitionViewModel> DataSetDefinitions { get; init; }

    public bool IsMobileDevice { get; init; }
}
