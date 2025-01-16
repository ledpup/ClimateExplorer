namespace ClimateExplorer.Data.Misc.NoaaGlobalTemp;

using ClimateExplorer.Core.Model;

public static class NoaaGlobalTemp
{
    public static DataFileMapping DataFileMapping()
    {
        var dataFileMapping = new DataFileMapping
        {
            DataSetDefinitionId = Guid.Parse("E61C6279-EDF4-461B-BDD1-0724D21F42F3"),
            LocationIdToDataFileMappings = new Dictionary<Guid, List<DataFileFilterAndAdjustment>>()
            {
                {
                    Region.RegionId(Region.Earth),
                    [
                        new ()
                        {
                            Id = "land_ocean.90S.90N",
                        },
                    ]
                },
                {
                    Region.RegionId("Land"),
                    [
                        new ()
                        {
                            Id = "land.90S.90N",
                        },
                    ]
                },
                {
                    Region.RegionId("Ocean"),
                    [
                        new ()
                        {
                            Id = "ocean.90S.90N",
                        },
                    ]
                },
                {
                    Region.RegionId(Region.NorthernHemi),
                    [
                        new ()
                        {
                            Id = "land_ocean.00N.90N",
                        },
                    ]
                },
                {
                    Region.RegionId(Region.SouthernHemi),
                    [
                        new ()
                        {
                            Id = "land_ocean.90S.00N",
                        },
                    ]
                },
                {
                    Region.RegionId(Region.Arctic),
                    [
                        new ()
                        {
                            Id = "land_ocean.60N.90N",
                        },
                    ]
                },
                {
                    Region.RegionId(Region.Antarctic),
                    [
                        new ()
                        {
                            Id = "land_ocean.90S.60S",
                        },
                    ]
                },
                {
                    Region.RegionId(Region.R60s60n),
                    [
                        new ()
                        {
                            Id = "land_ocean.60S.60N",
                        },
                    ]
                },
                {
                    Region.RegionId(Region.R60s60nOcean),
                    [
                        new ()
                        {
                            Id = "ocean.60S.60N",
                        },
                    ]
                },
                {
                    Region.RegionId(Region.NorthernHemiOcean),
                    [
                        new ()
                        {
                            Id = "ocean.00N.90N",
                        },
                    ]
                },
                {
                    Region.RegionId(Region.SouthernHemiOcean),
                    [
                        new ()
                        {
                            Id = "ocean.90S.00N",
                        },
                    ]
                },
                {
                    Region.RegionId(Region.ArcticOcean),
                    [
                        new ()
                        {
                            Id = "ocean.60N.90N",
                        },
                    ]
                },
                {
                    Region.RegionId(Region.AntarcticOcean),
                    [
                        new ()
                        {
                            Id = "ocean.90S.60S",
                        },
                    ]
                },
            },
        };
        return dataFileMapping;
    }
}
