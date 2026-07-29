namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

using ClimateExplorer.Core.Model;

public sealed record RecentObservationsContext(Guid Id, string Name, double? Latitude, IReadOnlyList<ObservationDomain> Domains)
{
    public static RecentObservationsContext? ForLocation(Location? location)
    {
        return location is null
            ? null
            : new RecentObservationsContext(location.Id, location.Name, location.Coordinates.Latitude, ObservationDomainCatalog.DefaultLocationDomains);
    }

    public static RecentObservationsContext ForAtmosphere()
    {
        return new RecentObservationsContext(Region.RegionId(Region.Atmosphere), Region.Atmosphere, null, ObservationDomainCatalog.AtmosphereDomains);
    }
}
