namespace ClimateExplorer.Core.Model;

public class GeographicalEntity
{
    /// <summary>
    /// The locations from the default location folder by Id, loaded once per process. Reading every location file
    /// to find one location cost tens of milliseconds, several times per dataset request.
    /// </summary>
    private static Task<Dictionary<Guid, Location>>? locationsById;

    public GeographicalEntity()
    {
    }

    public required Guid Id { get; set; }
    public required string Name { get; set; }

    /// <summary>
    /// Gets the region or location with this Id. A location is shared with other callers, so treat it as read-only.
    /// </summary>
    public static async Task<GeographicalEntity> GetGeographicalEntity(Guid id)
    {
        var region = Region.GetRegions().SingleOrDefault(x => x.Id == id);
        if (region is not null)
        {
            return region;
        }

        var locations = await GetLocationsById();
        return locations.TryGetValue(id, out var location)
            ? location
            : throw new InvalidOperationException($"No region or location has the Id {id}.");
    }

    public override string ToString()
    {
        return Name;
    }

    private static Task<Dictionary<Guid, Location>> GetLocationsById()
    {
        var task = locationsById;
        if (task is null || task.IsFaulted || task.IsCanceled)
        {
            task = LoadLocationsById();
            locationsById = task;
        }

        return task;
    }

    private static async Task<Dictionary<Guid, Location>> LoadLocationsById()
    {
        var locations = new Dictionary<Guid, Location>();
        foreach (var location in await Location.GetLocations())
        {
            locations.TryAdd(location.Id, location);
        }

        return locations;
    }
}
