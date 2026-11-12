namespace ClimateExplorer.Core;

public class Folders
{
    public const string SourceDataFolder = @"..\..\..\..\ClimateExplorer.SourceData\";
    public const string MetaDataFolder = @"..\..\..\..\ClimateExplorer.WebApi\MetaData\";
    public const string GhcnmFolder = @"..\..\..\..\ClimateExplorer.Data.Ghcnm\";

    // The GHCNm stations selected by ClimateExplorer.Data.Ghcnm. GHCNd and GHCNm precipitation import the same stations.
    public const string SelectedStationsFile = MetaDataFolder + @"Station\Stations_ghcnm_adjusted.json";
}
