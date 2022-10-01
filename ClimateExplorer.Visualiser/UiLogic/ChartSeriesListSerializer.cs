using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.UiModel;
using ClimateExplorer.Core.DataPreparation;
using System.Web;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Visualiser.UiLogic;

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

        string[] segments = s.Split(SeparatorsByLevel[0]);

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
        string[] segments = s.Split(SeparatorsByLevel[1]);

        return
            new ChartSeriesDefinition()
            {
                SeriesDerivationType = ParseEnum<SeriesDerivationTypes>(segments[0]),
                SourceSeriesSpecifications = ParseSourceSeriesSpecifications(segments[1], dataSetDefinitions, locations),
                Aggregation = ParseEnum<SeriesAggregationOptions>(segments[2]),
                RequestedColour = ParseEnum<Colours>(segments[3]),
                BinGranularity = ParseEnum<BinGranularities>(segments[4]),
                DisplayStyle = ParseEnum<SeriesDisplayStyle>(segments[5]),
                IsLocked = bool.Parse(segments[6]),
                ShowTrendline = bool.Parse(segments[7]),
                Smoothing = ParseEnum<SeriesSmoothingOptions>(segments[8]),
                SmoothingWindow = int.Parse(segments[9]),
                Value = ParseEnum<SeriesValueOptions>(segments[10]),
                Year = ParseNullableShort(segments[11]),
                IsExpanded = bool.Parse(segments[12]),
                SeriesTransformation = ParseEnum<SeriesTransformations>(segments[13]),
            };
    }

    static SourceSeriesSpecification[] ParseSourceSeriesSpecifications(string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IEnumerable<Location> locations)
    {
        string[] segments = s.Split(SeparatorsByLevel[2]);

        return
            segments
            .Select(x => ParseSourceSeriesSpecification(x, dataSetDefinitions, locations))
            .ToArray();
    }

    static SourceSeriesSpecification ParseSourceSeriesSpecification(string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IEnumerable<Location> locations)
    {
        string[] segments = s.Split(SeparatorsByLevel[3]);

        var dsd = dataSetDefinitions.Single(x => x.Id == Guid.Parse(segments[0]));
        var dt = (DataType?)ParseNullableEnum<DataType>(segments[1]);
        var da = (DataAdjustment?)ParseNullableEnum<DataAdjustment>(segments[2]);

        var md =
            dsd.MeasurementDefinitions
            .SingleOrDefault(
                x =>
                    x.DataAdjustment == da &&
                    x.DataType == dt);

        if (md == null)
        {
            throw new NullReferenceException($"Cannot find measurement definition in dataset {dsd.Id} with data type {dt} and data adjustment {da}.");
        }

        Location? l = null;

        if (segments[3].Length > 0)
        {
            l = locations.Single(x => x.Id == Guid.Parse(segments[3]));
        }

        return
            new SourceSeriesSpecification
            {
                DataSetDefinition = dsd,
                MeasurementDefinition = md,
                LocationId = l?.Id,
                LocationName = l?.Name,
            };
    }

    static readonly char[] SeparatorsByLevel = { ';', ',', '|', '*' };

    static string BuildSourceSeriesSpecificationsUrlComponent(SourceSeriesSpecification sss)
    {
        if (sss == null || sss.MeasurementDefinition == null || sss.DataSetDefinition == null)
        {
            throw new ArgumentNullException();
        }

        return
            string.Join(
                SeparatorsByLevel[3],
                sss.DataSetDefinition.Id,
                sss.MeasurementDefinition.DataType,
                sss.MeasurementDefinition.DataAdjustment,
                sss.LocationId
            );
    }

    static string BuildSourceSeriesSpecificationsUrlComponent(SourceSeriesSpecification[] sss)
    {
        return
            string.Join(
                SeparatorsByLevel[2],
                sss.Select(BuildSourceSeriesSpecificationsUrlComponent)
            );
    }


    static string BuildChartSeriesUrlComponent(ChartSeriesDefinition csd)
    {
        return
            string.Join(
                SeparatorsByLevel[1],
                csd.SeriesDerivationType,
                BuildSourceSeriesSpecificationsUrlComponent(csd.SourceSeriesSpecifications),
                csd.Aggregation,
                csd.RequestedColour,
                csd.BinGranularity,
                csd.DisplayStyle,
                csd.IsLocked,
                csd.ShowTrendline,
                csd.Smoothing,
                csd.SmoothingWindow,
                csd.Value,
                csd.Year,
                csd.IsExpanded,
                csd.SeriesTransformation
            );
    }

    public static string BuildChartSeriesListUrlComponent(List<ChartSeriesDefinition> chartSeriesList)
    {
        return
            string.Join(
                SeparatorsByLevel[0],
                chartSeriesList.Select(BuildChartSeriesUrlComponent)
            );
    }
}
