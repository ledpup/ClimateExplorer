namespace ClimateExplorer.Web.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.Core.DataPreparation;

public interface IExporter
{
    Stream ExportChartData(ILogger logger, IEnumerable<GeographicalEntity> locations, DataDownloadPackage dataDownloadPackage, string sourceUri);
}

public class Exporter : IExporter
{
    public Stream ExportChartData(ILogger logger, IEnumerable<GeographicalEntity> locations, DataDownloadPackage dataDownloadPackage, string sourceUri)
    {
        logger.LogInformation("ExportChartData got bin range " + dataDownloadPackage.Bins![0].ToString() + " to " + dataDownloadPackage.Bins.Last().ToString());

        var data = new List<string>
        {
            $"Exported from, \"{sourceUri}\"",
        };

        var locationIds = dataDownloadPackage.ChartSeriesWithData!.SelectMany(x => x.ChartSeries!.SourceSeriesSpecifications!).Select(x => x.LocationId).Distinct().ToArray();

        var relevantLocations = locationIds.Select(x => locations.Single(y => y.Id == x)).ToArray();

        foreach (var location in relevantLocations)
        {
            var text = location switch
            {
                Location l => $"{l.FullTitle},{l.Coordinates.ToFriendlyString(true)}",
                Region region => region.Name,
                _ => throw new NotImplementedException()
            };
            data.Add(text);
        }

        data.Add(string.Empty);

        var header = "Year," + string.Join(",", dataDownloadPackage.ChartSeriesWithData!.Select(x => BuildColumnHeader(relevantLocations, x.ChartSeries!)));
        data.Add(header);

        foreach (var bin in dataDownloadPackage.Bins)
        {
            var dataRow = bin.Label + ",";
            foreach (var cswd in dataDownloadPackage.ChartSeriesWithData!)
            {
                var val = cswd.ProcessedDataSet!.DataRecords.SingleOrDefault(x => x.BinId == bin.Id)?.Value;
                dataRow += (val == null ? string.Empty : val.Value.ToString("0.00")) + ",";
            }

            dataRow = dataRow.TrimEnd(',');
            data.Add(dataRow);
        }

        // Include a UTF-8 BOM to ensure degrees symbol is handled correctly when exported CSV is opened by excel
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(data.SelectMany(s => System.Text.Encoding.UTF8.GetBytes(s + Environment.NewLine))).ToArray();
        var fileStream = new MemoryStream(bytes);

        return fileStream;
    }

    private string BuildColumnHeader(GeographicalEntity[] relevantLocations, ChartSeriesDefinition csd)
    {
        var s = string.Empty;

        bool includeLocationInColumnHeader = relevantLocations.Length > 1;

        return csd.SeriesDerivationType switch
        {
            SeriesDerivationTypes.ReturnSingleSeries => s + BuildColumnHeader(includeLocationInColumnHeader, csd.SourceSeriesSpecifications!.Single(), relevantLocations),
            SeriesDerivationTypes.DifferenceBetweenTwoSeries => s + BuildColumnHeader(includeLocationInColumnHeader, csd.SourceSeriesSpecifications!.First(), relevantLocations) + " minus " + BuildColumnHeader(includeLocationInColumnHeader, csd.SourceSeriesSpecifications!.Last(), relevantLocations),
            SeriesDerivationTypes.AverageOfMultipleSeries => s + BuildColumnHeader(includeLocationInColumnHeader, csd.SourceSeriesSpecifications!.First(), relevantLocations) + " average " + BuildColumnHeader(includeLocationInColumnHeader, csd.SourceSeriesSpecifications!.Last(), relevantLocations),
            _ => throw new NotImplementedException($"SeriesDerivationType {csd.SeriesDerivationType}"),
        };
    }

    private string BuildColumnHeader(bool includeLocationInColumnHeader, SourceSeriesSpecification sss, GeographicalEntity[] relevantLocations)
    {
        string s = string.Empty;

        if (includeLocationInColumnHeader)
        {
            s += relevantLocations.Single(x => x.Id == sss.LocationId).Name + " ";
        }

        s += $"{sss.MeasurementDefinition!.DataType} {sss.MeasurementDefinition.DataAdjustment}";

        return s;
    }
}
