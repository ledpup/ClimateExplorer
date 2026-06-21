namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;

public sealed record ChartLocationSubstitutionContext
{
    public required ChartState State { get; init; }
    public required Location Location { get; init; }
    public required IReadOnlyList<Region> Regions { get; init; }
    public required IReadOnlyList<DataSetDefinitionViewModel> DataSetDefinitions { get; init; }
}
