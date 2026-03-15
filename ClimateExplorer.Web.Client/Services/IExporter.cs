using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.UiModel;

public interface IExporter
{
    Stream ExportChartData(ILogger logger, IEnumerable<GeographicalEntity> locations, DataDownloadPackage dataDownloadPackage, string sourceUri);
}