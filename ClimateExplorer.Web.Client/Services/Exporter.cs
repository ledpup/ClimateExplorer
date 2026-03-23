namespace ClimateExplorer.Web.Client.Services;

using System.Globalization;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

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
                _ => throw new NotImplementedException(),
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

    public Stream ExportClimateRecords(ILogger logger, Location location, ClimateRecord[] records, string sourceUri)
    {
        logger.LogInformation("ExportClimateRecords for {Location} with {Count} records", location.FullTitle, records.Length);

        var baseUri = new Uri(sourceUri);
        var locationUrl = $"{baseUri.GetLeftPart(UriPartial.Authority)}/location/{location.UrlReadyName()}";

        var data = new List<string>
        {
            $"Exported from, \"{locationUrl}\"",
            $"{location.FullTitle},{location.Coordinates.ToFriendlyString(true)}",
            string.Empty,
        };

        if (records.Length > 0)
        {
            var resolution = records[0].DataResolution;
            var unitLabel = UnitOfMeasureLabelShort(records[0].UnitOfMeasure);

            data.Add(resolution switch
            {
                DataResolution.Yearly => $"Rank,Year,Anomaly ({unitLabel}),Average ({unitLabel})",
                DataResolution.Monthly => $"Rank,Month,Year,Average ({unitLabel})",
                _ => $"Rank,Day Month,Year,Value ({unitLabel})",
            });

            int rank = 1;
            foreach (var record in records)
            {
                data.Add(resolution switch
                {
                    DataResolution.Yearly =>
                        $"{rank},{record.Year},{(record.Anomaly.HasValue ? FormatCsvValue(record.Anomaly.Value) : string.Empty)},{(record.Average.HasValue ? FormatCsvValue(record.Average.Value) : string.Empty)}",
                    DataResolution.Monthly =>
                        $"{rank},{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(record.Month)},{record.Year},{FormatCsvValue(record.Value)}",
                    _ =>
                        $"{rank},{(record.Day.HasValue ? $"{record.Day} {CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(record.Month)}" : CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(record.Month))},{record.Year},{FormatCsvValue(record.Value)}",
                });
                rank++;
            }
        }

        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(data.SelectMany(s => System.Text.Encoding.UTF8.GetBytes(s + Environment.NewLine))).ToArray();
        return new MemoryStream(bytes);
    }

    private static string FormatCsvValue(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    private string BuildColumnHeader(GeographicalEntity[] relevantLocations, ChartSeriesDefinition csd)
    {
        var s = string.Empty;

        bool includeLocationInColumnHeader = relevantLocations.Length > 1;

        switch (csd.SeriesDerivationType)
        {
            case SeriesDerivationTypes.ReturnSingleSeries:
                return s + BuildColumnHeader(includeLocationInColumnHeader, csd.SourceSeriesSpecifications!.Single());

            case SeriesDerivationTypes.DifferenceBetweenTwoSeries:
                return s + BuildColumnHeader(includeLocationInColumnHeader, csd.SourceSeriesSpecifications!.First()) + " minus " + BuildColumnHeader(includeLocationInColumnHeader, csd.SourceSeriesSpecifications!.Last());

            case SeriesDerivationTypes.AverageOfMultipleSeries:
                return s + BuildColumnHeader(includeLocationInColumnHeader, csd.SourceSeriesSpecifications!.First()) + " average " + BuildColumnHeader(includeLocationInColumnHeader, csd.SourceSeriesSpecifications!.Last());

            default:
                throw new NotImplementedException($"SeriesDerivationType {csd.SeriesDerivationType}");
        }
    }

    private string BuildColumnHeader(bool includeLocationInColumnHeader, SourceSeriesSpecification sss)
    {
        string s = string.Empty;

        if (includeLocationInColumnHeader)
        {
            s += sss.LocationName + " ";
        }

        s += $"{sss.MeasurementDefinition!.DataType} {sss.MeasurementDefinition.DataAdjustment}";

        return s;
    }
}
