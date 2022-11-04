using ClimateExplorer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static ClimateExplorer.Core.Enums;


public class DataSetDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string Description { get; set; }
    public string Publisher { get; set; }
    public string PublisherUrl { get; set; }
    public string MoreInformationUrl { get; set; }
    public string StationInfoUrl { get; set; }
    public string LocationInfoUrl { get; set; }
    public string DataDownloadUrl { get; set; }

    public List<MeasurementDefinition> MeasurementDefinitions { get; set; }

    public DataFileLocationMapping DataLocationMapping { get; set; }

    public static async Task<List<DataSetDefinition>> GetDataSetDefinitions(string filePath = @"MetaData\DataSetDefinitions.json", string dataFileLocationMappingFolder = null)
    {
        var text = await File.ReadAllTextAsync(filePath);
        var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
        var ddds = JsonSerializer.Deserialize<List<DataSetDefinition>>(text, options);

        var dataFileLocationMappings = await DataFileLocationMapping.GetDataFileLocationMappings();
        foreach (var ddd in ddds)
        {
            var dataFileLocationMapping = dataFileLocationMappings.SingleOrDefault(x => x.DataSetDefinitionId == ddd.Id);
            ddd.DataLocationMapping = dataFileLocationMapping;
        }

        return ddds;
    }

    public override string ToString()
    {
        return "DSD " + Name;
    }
}

