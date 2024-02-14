namespace ClimateExplorer.Core.Model;

using System.Text.Json;

public class DataFileMapping
{
    public DataFileMapping()
    {
        LocationIdToDataFileMappings = [];
    }

    public Guid? DataSetDefinitionId { get; set; }
    public Guid? MeasurementDefinitionId { get; set; }
    public Dictionary<Guid, List<DataFileFilterAndAdjustment>> LocationIdToDataFileMappings { get; set; }

    public static async Task<DataFileMapping> GetDataFileMappingFromFile(string pathAndFileName)
    {
        var text = await File.ReadAllTextAsync(pathAndFileName);
        var dataFileMapping = JsonSerializer.Deserialize<DataFileMapping>(text);
        return dataFileMapping!;
    }

    public static async Task<List<DataFileMapping>> GetDataFileMappings(string? dataFileMappingFolder = null)
    {
        dataFileMappingFolder = dataFileMappingFolder ?? @"MetaData\DataFileMapping";
        var dataFileMappings = new List<DataFileMapping>();
        var files = Directory.GetFiles(dataFileMappingFolder).ToList();
        foreach (var file in files)
        {
            var dataFileMapping = await GetDataFileMappingFromFile(file);
            dataFileMappings.Add(dataFileMapping);
        }

        return dataFileMappings;
    }
}
