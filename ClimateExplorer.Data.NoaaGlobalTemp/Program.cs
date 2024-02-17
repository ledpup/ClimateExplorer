// See https://aka.ms/new-console-template for more information
using ClimateExplorer.Core.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("Hello, World!");

var stations = new List<Station>()
{
    new() {
        Id = "land_ocean.90S.90N",
    },
    new() {
        Id = "land.90S.90N",
    },
    new() {
        Id = "ocean.90S.90N",
    },

    new() {
        Id = "land_ocean.00N.90N",
    },
    new() {
        Id = "land_ocean.90S.00N",
    },

    new() {
        Id = "land_ocean.60N.90N",
    },
    new() {
        Id = "land_ocean.90S.60S",
    },
    new() {
        Id = "land_ocean.60S.60N",
    },
};



var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

Directory.CreateDirectory("Output");

File.WriteAllText(@"Output\Stations_NoaaGlobalTemp.json", JsonSerializer.Serialize(stations, options));
File.WriteAllText(@"Output\DataFileMapping_NoaaGlobalTemp.json", JsonSerializer.Serialize(NoaaGlobalTemp.DataFileMapping(), options));

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
                new()
                {
                    new()
                    {
                        Id = "land_ocean.90S.90N",
                    }
                }
            },
            {
                Region.RegionId("Land"),
                new()
                {
                    new()
                    {
                        Id = "land.90S.90N",
                    }
                }
            },
            {
            Region.RegionId("Ocean"),
                new()
                {
                    new()
                    {
                        Id = "ocean.90S.90N",
                    }
                }
            },
            {
                Region.RegionId(Region.NorthernHemi),
                new()
                {
                    new()
                    {
                        Id = "land_ocean.00N.90N",
                    }
                }
            },
            {
            Region.RegionId(Region.SouthernHemi),
                new()
                {
                    new()
                    {
                        Id = "land_ocean.90S.00N",
                    }
                }
            },
            {
                Region.RegionId(Region.Arctic),
                new()
                {
                    new()
                    {
                        Id = "land_ocean.60N.90N",
                    }
                }
            },
            {
                Region.RegionId(Region.Antarctic),
                new()
                {
                    new()
                    {
                        Id = "land_ocean.90S.60S",
                    }
                }
            },
            {
                Region.RegionId(Region.R60s60n),
                new()
                {
                    new()
                    {
                        Id = "land_ocean.60S.60N",
                    }
                }
            },
            {
                Region.RegionId(Region.R60s60nOcean),
                new()
                {
                    new()
                    {
                        Id = "ocean.60S.60N",
                    }
                }
            },
            {
                Region.RegionId(Region.NorthernHemiOcean),
                new()
                {
                    new()
                    {
                        Id = "ocean.00N.90N",
                    }
                }
            },
            {
                Region.RegionId(Region.SouthernHemiOcean),
                new()
                {
                    new()
                    {
                        Id = "ocean.90S.00N",
                    }
                }
            },
            {
                Region.RegionId(Region.ArcticOcean),
                new()
                {
                    new()
                    {
                        Id = "ocean.60N.90N",
                    }
                }
            },
            {
                Region.RegionId(Region.AntarcticOcean),
                new()
                {
                    new()
                    {
                        Id = "ocean.90S.60S",
                    }
                }
            },
        }
        };
        return dataFileMapping;
    }
}