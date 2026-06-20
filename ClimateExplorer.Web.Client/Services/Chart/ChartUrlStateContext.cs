namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;

public sealed record ChartUrlStateContext
{
    public IDictionary<Guid, Location>? Locations { get; init; }

    public required IReadOnlyList<Region> Regions { get; init; }

    public required IReadOnlyList<DataSetDefinitionViewModel> DataSetDefinitions { get; init; }
}
