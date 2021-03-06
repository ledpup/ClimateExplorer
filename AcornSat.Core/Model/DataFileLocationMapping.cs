using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AcornSat.Core.Model;

public class DataFileLocationMapping
{
    public DataFileLocationMapping()
    {
        LocationIdToDataFileMappings = new Dictionary<Guid, List<DataFileFilterAndAdjustment>>();
    }

    public Guid? DataSetDefinitionId { get; set; }
    public Guid? MeasurementDefinitionId { get; set; }
    public Dictionary<Guid, List<DataFileFilterAndAdjustment>> LocationIdToDataFileMappings { get; set; }

    public static async Task<DataFileLocationMapping> GetDataFileLocationMappingFromFile(string pathAndFileName)
    {
        var text = await File.ReadAllTextAsync(pathAndFileName);
        var dataFileLocationMapping = JsonSerializer.Deserialize<DataFileLocationMapping>(text);
        return dataFileLocationMapping;
    }

    public static async Task<List<DataFileLocationMapping>> GetDataFileLocationMappings(string? dataFileLocationMappingFolder = null)
    {
        dataFileLocationMappingFolder = dataFileLocationMappingFolder ?? @"MetaData\DataFileLocationMapping";
        var dataFileLocationMappings = new List<DataFileLocationMapping>();
        var files = Directory.GetFiles(dataFileLocationMappingFolder).ToList();
        foreach (var file in files)
        {
            var dataFileLocationMapping = await GetDataFileLocationMappingFromFile(file);
            dataFileLocationMappings.Add(dataFileLocationMapping);
        }
        return dataFileLocationMappings;
    }
}
