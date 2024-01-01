using ClimateExplorer.Core.DataPreparation;

namespace ClimateExplorer.Core.Model;

public class GeographicalEntity
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }

    public GeographicalEntity() { }

    public override string ToString()
    {
        return Name;
    }

    public static async Task<GeographicalEntity> GetGeographicalEntity(Guid id)
    {
        var geoEntity = Region.GetRegions().SingleOrDefault(x => x.Id == id) as GeographicalEntity;
        if (geoEntity == null)
        {
            geoEntity = (await Location.GetLocations()).Single(x => x.Id == id);
        }

        return geoEntity;
    }
}
