namespace ClimateExplorer.Core.Model;
public class DataSetDefinition
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public string? PublisherUrl { get; set; }
    public string? MoreInformationUrl { get; set; }
    public string? StationInfoUrl { get; set; }
    public string? LocationInfoUrl { get; set; }
    public string? DataDownloadUrl { get; set; }

    public List<MeasurementDefinition>? MeasurementDefinitions { get; set; }

    public DataFileMapping? DataLocationMapping { get; set; }

    public static async Task<List<DataSetDefinition>> GetDataSetDefinitions()
    {
        var ddds = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

        var dataFileMappings = await DataFileMapping.GetDataFileMappings();
        foreach (var ddd in ddds!)
        {
            var dataFileMapping = dataFileMappings.SingleOrDefault(x => x.DataSetDefinitionId == ddd.Id);
            ddd.DataLocationMapping = dataFileMapping;
        }

        return ddds;
    }

    public override string ToString()
    {
        return "DSD " + Name;
    }
}
