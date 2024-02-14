namespace ClimateExplorer.Core.Model;

public class ApiMetadataModel
{
    required public string Version { get; set; }
    public DateTime BuildTimeUtc { get; set; }
}
