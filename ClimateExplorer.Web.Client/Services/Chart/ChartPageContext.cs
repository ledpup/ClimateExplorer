namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;

public sealed record ChartPageContext
{
    public required ChartPageKind PageKind { get; init; }
    public Location? Location { get; init; }
    public IDictionary<Guid, Location>? Locations { get; init; }
    public required IReadOnlyList<Region> Regions { get; init; }
    public required IReadOnlyList<DataSetDefinitionViewModel> DataSetDefinitions { get; init; }
    public bool IsMobileDevice { get; init; }
}
