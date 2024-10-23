namespace ClimateExplorer.Core.Model;

public class GeographicalEntity
{
    public GeographicalEntity()
    {
    }

    required public Guid Id { get; set; }
    required public string Name { get; set; }

    public static async Task<GeographicalEntity> GetGeographicalEntity(Guid id)
    {
        var geoEntity = (await Region.GetRegions()).SingleOrDefault(x => x.Id == id) as GeographicalEntity;
        if (geoEntity == null)
        {
            geoEntity = (await Location.GetLocations()).Single(x => x.Id == id);
        }

        return geoEntity;
    }

    public override string ToString()
    {
        return Name;
    }
}
