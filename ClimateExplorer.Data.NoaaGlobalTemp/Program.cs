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
};

var dataFileMapping = new DataFileMapping
{
    DataSetDefinitionId = Guid.Parse("E61C6279-EDF4-461B-BDD1-0724D21F42F3"),
    LocationIdToDataFileMappings = new Dictionary<Guid, List<DataFileFilterAndAdjustment>>()
    {
        {
            Region.RegionId("Earth"),
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
        }
    }
};

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

Directory.CreateDirectory("Output");

File.WriteAllText(@"Output\Stations_NoaaGlobalTemp.json", JsonSerializer.Serialize(stations, options));
File.WriteAllText(@"Output\DataFileMapping_NoaaGlobalTemp.json", JsonSerializer.Serialize(dataFileMapping, options));