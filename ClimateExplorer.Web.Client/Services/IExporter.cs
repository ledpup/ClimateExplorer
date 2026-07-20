namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

public interface IExporter
{
    Stream ExportChartData(ILogger logger, IEnumerable<GeographicalEntity> locations, DataDownloadPackage dataDownloadPackage, string sourceUri);
    Stream ExportClimateRecords(ILogger logger, Location location, ClimateRecordViewModel[] records, string sourceUri, DataResolution dataResolution, UnitOfMeasure unitOfMeasure);
    Stream ExportTrendData(ILogger logger, Location location, string dataTypeLabel, string windowLabel, IReadOnlyList<DataPoint> points, string sourceUri);
}
