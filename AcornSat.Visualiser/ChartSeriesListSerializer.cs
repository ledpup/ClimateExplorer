using AcornSat.Core.ViewModel;
using AcornSat.Visualiser.UiModel;
using System.Web;
using static AcornSat.Core.Enums;

namespace AcornSat.Visualiser
{
    public static class ChartSeriesListSerializer
    {
        static short? ParseNullableShort(string s)
        {
            if (String.IsNullOrWhiteSpace(s)) return null;

            if (short.TryParse(s, out short val))
            {
                return val;
            }

            throw new Exception($"Failed to parse '{s}'");
        }

        static object ParseNullableEnum<T>(string s) 
        {
            if (String.IsNullOrWhiteSpace(s)) return null;

            return (T)Enum.Parse(typeof(T), s);
        }

        static T ParseEnum<T>(string s) where T : notnull, System.Enum
        {
            return (T)Enum.Parse(typeof(T), s);
        }

        public static List<ChartSeriesDefinition> ParseChartSeriesDefinitionList(ILogger logger, string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IEnumerable<Location> locations)
        {
            logger.LogInformation("ParseChartSeriesDefinitionList: " + s);

            string[] segments = s.Split(';');

            var seriesList =
                segments
                .Select(x => ParseChartSeriesUrlComponent(logger, x, dataSetDefinitions, locations))
                .Where(x => x != null)
                .ToList();

            logger.LogInformation("returning seriesList with " + seriesList.Count + "elements");

            return seriesList;
        }

        static ChartSeriesDefinition ParseChartSeriesUrlComponent(ILogger logger, string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IEnumerable<Location> locations)
        {
            logger.LogInformation("Parsing s: " + s);

            string[] segments = s.Split('|');

            logger.LogInformation("segments.Length: " + segments.Length);

            var dsd = dataSetDefinitions.Single(x => x.Id == Guid.Parse(segments[0]));

            var da = (DataAdjustment?)ParseNullableEnum<DataAdjustment>(segments[7]);
            var dt = (DataType?)ParseNullableEnum<DataType>(segments[8]);

            logger.LogInformation(da + " " + dt);

            logger.LogInformation(dsd.Id.ToString());

            var md =
                dsd.MeasurementDefinitions
                .SingleOrDefault(
                    x =>
                        x.DataAdjustment == da &&
                        x.DataType == dt);

            if (md == null) return null;

            Location? l = null;
            
            if (segments[1].Length > 0)
            {
                l = locations.Single(x => x.Id == Guid.Parse(segments[1]));
            }

            return
                new ChartSeriesDefinition()
                {
                    DataSetDefinition = dsd,
                    LocationId = l?.Id,
                    LocationName = l?.Name,
                    Aggregation = ParseEnum<SeriesAggregationOptions>(segments[2]),
                    Colour = HttpUtility.UrlDecode(segments[3]),
                    DataResolution = ParseEnum<DataResolution>(segments[4]),
                    DisplayStyle = ParseEnum<SeriesDisplayStyle>(segments[5]),
                    IsLocked = bool.Parse(segments[6]),
                    MeasurementDefinition = md,
                    ShowTrendline = bool.Parse(segments[9]),
                    Smoothing = ParseEnum<SeriesSmoothingOptions>(segments[10]),
                    SmoothingWindow = int.Parse(segments[11]),
                    Value = ParseEnum<SeriesValueOptions>(segments[12]),
                    Year = ParseNullableShort(segments[13])
                };
        }

        static string BuildChartSeriesUrlComponent(ChartSeriesDefinition csd)
        {
            if (csd.DataSetDefinition == null) throw new Exception("DataSetDefinition unset on ChartSeriesDefinition");
            if (csd.MeasurementDefinition == null) throw new Exception("MeasurementDefinition unset on ChartSeriesDefinition");

            return
                string.Join(
                    "|",
                    // Just enough DataSetDefinition fields that we can identify the correct one when we deserialize
                    csd.DataSetDefinition.Id,
                    csd.LocationId,
                    csd.Aggregation,
                    HttpUtility.UrlEncode(csd.Colour),
                    csd.DataResolution,
                    csd.DisplayStyle,
                    csd.IsLocked,
                    // Just enough MeasurementDefinition fields that we can identify the correct one when we deserialize
                    csd.MeasurementDefinition.DataAdjustment,
                    csd.MeasurementDefinition.DataType,
                    csd.ShowTrendline,
                    csd.Smoothing,
                    csd.SmoothingWindow,
                    csd.Value,
                    csd.Year
                );
        }

        public static string BuildChartSeriesListUrlComponent(List<ChartSeriesDefinition> chartSeriesList)
        {
            var fragments = chartSeriesList.Select(BuildChartSeriesUrlComponent);

            return string.Join(";", fragments);
        }
    }
}
