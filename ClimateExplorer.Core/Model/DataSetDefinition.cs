namespace ClimateExplorer.Core.Model;
public class DataSetDefinition
{
    /// <summary>
    /// The definitions read from the default mapping folder. The mapping files are a few MB of JSON that don't change
    /// while the process runs, and the API reads them on every dataset request, so they're loaded once.
    /// </summary>
    private static Task<List<DataSetDefinition>>? defaultDataSetDefinitions;

    public required Guid Id { get; set; }
    public required string Name { get; init; }
    public required string ShortName { get; init; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public string? PublisherUrl { get; set; }
    public string? PublisherDescription { get; set; }
    public string? PublisherDivision { get; set; }
    public string? MoreInformationUrl { get; set; }
    public string? StationInfoUrl { get; set; }
    public string? LocationInfoUrl { get; set; }
    public string? DataDownloadUrl { get; set; }
    public string? DataDownloaderKey { get; set; }
    public string? StationMetadataFileName { get; set; }
    public List<MeasurementDefinition>? MeasurementDefinitions { get; set; }

    public DataFileMapping? DataLocationMapping { get; set; }

    /// <summary>
    /// Gets the definitions from the default mapping folder, loaded once per process. Callers share the definition
    /// objects, so treat them as read-only; the list itself is a fresh copy.
    /// </summary>
    public static async Task<List<DataSetDefinition>> GetDataSetDefinitions()
    {
        var task = defaultDataSetDefinitions;
        if (task is null || task.IsFaulted || task.IsCanceled)
        {
            task = GetDataSetDefinitions(null);
            defaultDataSetDefinitions = task;
        }

        return [.. await task];
    }

    public static async Task<List<DataSetDefinition>> GetDataSetDefinitions(string? dataFileMappingFolder)
    {
        var ddds = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

        var dataFileMappings = await DataFileMapping.GetDataFileMappings(dataFileMappingFolder);
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
