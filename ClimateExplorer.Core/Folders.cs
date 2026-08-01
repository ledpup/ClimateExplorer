namespace ClimateExplorer.Core;

public class Folders
{
    public const string SourceDataFolder = @"..\..\..\..\ClimateExplorer.SourceData\";
    public const string MetaDataFolder = @"..\..\..\..\ClimateExplorer.WebApi\MetaData\";
    public const string GhcnmFolder = @"..\..\..\..\ClimateExplorer.Data.Ghcnm\";
    public const string SelectedStationsFile = @"..\..\..\..\ClimateExplorer.Data.Ghcnm\bin\Debug\net10.0\Output\MetaData\selected-stations.json";

    /// <summary>
    /// The checked-in GHCN station metadata the site serves. Unlike <see cref="SelectedStationsFile"/>,
    /// which is a build output of ClimateExplorer.Data.Ghcnm and only exists after that tool has been run,
    /// this is the published set - so it is what a downstream tool should reconcile against if it needs
    /// the stations the site actually has.
    /// </summary>
    public const string GhcnStationMetadataFile = MetaDataFolder + @"Station\Stations_ghcnm_adjusted.json";
}
