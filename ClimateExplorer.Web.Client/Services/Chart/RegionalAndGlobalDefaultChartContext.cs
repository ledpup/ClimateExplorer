namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Core.ViewModel;

public sealed record RegionalAndGlobalDefaultChartContext
{
    public required IReadOnlyList<DataSetDefinitionViewModel> DataSetDefinitions { get; init; }
}
