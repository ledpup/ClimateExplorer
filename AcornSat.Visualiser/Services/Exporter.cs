using AcornSat.Core.Model;
using AcornSat.Visualiser.UiModel;
using ClimateExplorer.Core.DataPreparation;

namespace AcornSat.Visualiser.Services;

public interface IExporter
{
    Stream ExportChartData(ILogger logger, List<SeriesWithData> chartSeriesWithData, IEnumerable<Location> locations, BinIdentifier startBin, BinIdentifier endBin, string sourceUri);
}

public class Exporter : IExporter
{
    public Stream ExportChartData(ILogger logger, List<SeriesWithData> chartSeriesWithData, IEnumerable<Location> locations, BinIdentifier startBin, BinIdentifier endBin, string sourceUri)
    {
        logger.LogInformation("ExportChartData from " + startBin.ToString() + " to " + endBin.ToString());

        var bins = BinHelpers.EnumerateBinsInRange(startBin, endBin).ToArray();

        logger.LogInformation("ExportChartData got bin range " + bins[0].ToString() + " to " + bins.Last().ToString());

        var data = new List<string>();

        data.Add("Exported from," + sourceUri);

        var locationIds = chartSeriesWithData.Select(x => x.ChartSeries.LocationId).Where(x => x != null).Distinct().ToArray();

        var relevantLocations = locationIds.Select(x => locations.Single(y => y.Id == x)).ToArray();

        foreach (var location in relevantLocations)
        {
            data.Add($"{location.Name},{location.Coordinates.ToString(true)}");
        }

        data.Add(string.Empty);

        var header = "Year," + string.Join(",", chartSeriesWithData.Select(x => BuildColumnHeader(relevantLocations, x.ChartSeries)));
        data.Add(header);

        foreach (var bin in bins)
        {
            var dataRow = bin.Label + ",";
            foreach (var cswd in chartSeriesWithData)
            {
                var val = cswd.ProcessedDataSet.DataRecords.SingleOrDefault(x => x.BinId == bin.Id)?.Value;
                dataRow += (val == null ? string.Empty : MathF.Round((float)val, 2).ToString("0.00")) + ",";
            }
            dataRow = dataRow.TrimEnd(',');
            data.Add(dataRow);
        }

        // Include a UTF-8 BOM to ensure degrees symbol is handled correctly when exported CSV is opened by excel
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(data.SelectMany(s => System.Text.Encoding.UTF8.GetBytes(s + Environment.NewLine))).ToArray();
        var fileStream = new MemoryStream(bytes);

        return fileStream;
    }

    string BuildColumnHeader(Location[] relevantLocations, ChartSeriesDefinition csd)
    {
        var s = "";

        if (relevantLocations.Length > 1 && csd.LocationName != null) s += csd.LocationName + " ";

        s += $"{csd.MeasurementDefinition.DataType} {csd.MeasurementDefinition.DataAdjustment}";

        return s;
    }
}
