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
    public bool? AlterDownloadedFile { get; set; }

    public List<MeasurementDefinition>? MeasurementDefinitions { get; set; }

    public DataFileMapping? DataLocationMapping { get; set; }

    public static async Task<List<DataSetDefinition>> GetDataSetDefinitions()
    {
        var ddds = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

        var dataFileMappings = await DataFileMapping.GetDataFileMappings();
        foreach (var ddd in ddds!)
        {
            var dataFileMapping = dataFileMappings.Where(x => x.DataSetDefinitionId == ddd.Id);
            if (dataFileMapping.Count() > 1)
            {
                throw new Exception($"More than one data file mapping found for data set definition ID {ddd.Id}. Only one is permitted");
            }

            ddd.DataLocationMapping = dataFileMapping.SingleOrDefault();
        }

        return ddds;
    }

    public override string ToString()
    {
        return "DSD " + Name;
    }
}
