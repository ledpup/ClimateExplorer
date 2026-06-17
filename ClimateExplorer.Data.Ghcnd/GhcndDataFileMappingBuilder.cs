namespace ClimateExplorer.Data.Ghcnd;

using System.Text.Json;
using System.Text.Json.Serialization;
using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;

public static class GhcndDataFileMappingBuilder
{
    public static async Task CreateDataFileMapping(List<Station> stationsWithData, string dataType, string dataSetDefinitionId)
    {
        var dataFileMapping = new DataFileMapping
        {
            DataSetDefinitionId = Guid.Parse(dataSetDefinitionId),
            LocationIdToDataFileMappings = []
        };

        var ghcnIdToLocationIds = await GetGhcnIdToLocationIds();

        stationsWithData.ForEach(x =>
        {
            dataFileMapping.LocationIdToDataFileMappings.Add(
                ghcnIdToLocationIds[x.Id],
                [
                    new()
                    {
                        Id = x.Id
                    }
                ]);
        });

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        File.WriteAllText(Folders.MetaDataFolder + $@"DataFileMapping\DataFileMapping_ghcnd_{dataType}.json", JsonSerializer.Serialize(dataFileMapping, jsonSerializerOptions));
    }

    private static async Task<Dictionary<string, Guid>> GetGhcnIdToLocationIds()
    {
        const string ghcnIdToLocationIdsFile = Folders.GhcnmFolder + @"MetaData\GhcnIdToLocationIds.json";
        if (File.Exists(ghcnIdToLocationIdsFile))
        {
            var contents = await File.ReadAllTextAsync(ghcnIdToLocationIdsFile);
            var ghcnIdToLocationIds = JsonSerializer.Deserialize<Dictionary<string, Guid>>(contents);
            return ghcnIdToLocationIds!;
        }

        throw new Exception($"Expecting {ghcnIdToLocationIdsFile} to exist");
    }
}
