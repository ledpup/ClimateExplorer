namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.UiModel;

public interface IExporter
{
    Stream ExportChartData(ILogger logger, IEnumerable<GeographicalEntity> locations, DataDownloadPackage dataDownloadPackage, string sourceUri);
    Stream ExportClimateRecords(ILogger logger, Location location, ClimateRecord[] records, string sourceUri);
}