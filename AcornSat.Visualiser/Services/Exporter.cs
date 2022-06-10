using AcornSat.Visualiser.UiModel;

namespace AcornSat.Visualiser.Services
{
    public interface IExporter
    {
        Stream ExportChartData(List<SeriesWithData> chartSeriesWithData, IEnumerable<Location> locations, string[] years);
    }

    public class Exporter : IExporter
    {
        public Stream ExportChartData(List<SeriesWithData> chartSeriesWithData, IEnumerable<Location> locations, string[] years)
        {
            var data = new List<string>();

            var locationIds = chartSeriesWithData.Select(x => x.ChartSeries.LocationId).Where(x => x != null).Distinct().ToArray();

            var relevantLocations = locationIds.Select(x => locations.Single(y => y.Id == x)).ToArray();

            foreach (var location in relevantLocations)
            {
                data.Add($"{location.Name},{location.Coordinates.ToString(true)}");
            }

            // TODO: This is now series-level information (i.e. one per column, not one per export) - how should we include this?
            //            var resolution = SelectedChartType == ChartType.Line ? "Yearly average" : "Yearly values relative to average";
            //data.Add($"{resolution},{ChartStartYear}-{ChartEndYear},Averaging method: {DayGroupingText(SelectedDayGrouping).ToLower()} with a threshold of {SelectedDayGroupThreshold},average requiring all groupings for a full data set - otherwise record null");

            data.Add(string.Empty);

            var header = "Year," + string.Join(",", chartSeriesWithData.Select(x => BuildColumnHeader(relevantLocations, x.ChartSeries)));
            data.Add(header);

            foreach (var year in years)
            {
                var dataRow = year + ",";
                foreach (var cswd in chartSeriesWithData)
                {
                    var pds = cswd.ProcessedDataSets.Single();

                    var dataRecord = pds.DataRecords.Single(x => x.Year == short.Parse(year));
                    dataRow += (dataRecord.Value == null ? string.Empty : MathF.Round((float)dataRecord.Value, 2).ToString("0.00")) + ",";
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
}
