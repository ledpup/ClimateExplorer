namespace ClimateExplorer.Web.UiLogic;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

public static class ChartSeriesListSerializer
{
    private static readonly char[] SeparatorsByLevel = [';', ',', '|', '*'];

    public static List<ChartSeriesDefinition> ParseChartSeriesDefinitionList(ILogger logger, string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IDictionary<Guid, Location>? locations, IEnumerable<Region> regions)
    {
        logger.LogInformation("ParseChartSeriesDefinitionList: " + s);

        string[] segments = s.Split(SeparatorsByLevel[0]);

        var seriesList =
            segments
            .Select(x => ParseChartSeriesUrlComponent(logger, x, dataSetDefinitions, locations, regions))
            .Where(x => x != null)
            .ToList();

        logger.LogInformation($"Returning seriesList with {seriesList.Count} elements");

        return seriesList;
    }

    public static string BuildChartSeriesListUrlComponent(List<ChartSeriesDefinition> chartSeriesList)
    {
        return
            string.Join(
                SeparatorsByLevel[0],
                chartSeriesList.Select(BuildChartSeriesUrlComponent));
    }

    private static short? ParseNullableShort(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        if (short.TryParse(s, out short val))
        {
            return val;
        }

        throw new Exception($"Failed to parse '{s}'");
    }

    private static object ParseNullableEnum<T>(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return null!;
        }

        return (T)Enum.Parse(typeof(T), s);
    }

    private static T ParseEnum<T>(string s)
        where T : notnull, System.Enum
    {
        return (T)Enum.Parse(typeof(T), s);
    }

    private static float? ParseNullableFloat(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        if (float.TryParse(s, out float val))
        {
            return val;
        }

        throw new Exception($"Failed to parse '{s}'");
    }

    private static ChartSeriesDefinition ParseChartSeriesUrlComponent(ILogger logger, string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IDictionary<Guid, Location>? locations, IEnumerable<Region> regions)
    {
        string[] segments = s.Split(SeparatorsByLevel[1]);

        var dr = (DataResolution?)ParseNullableEnum<DataResolution>(segments[18]);

        return
            new ChartSeriesDefinition()
            {
                SeriesDerivationType = ParseEnum<SeriesDerivationTypes>(segments[0]),
                SourceSeriesSpecifications = ParseSourceSeriesSpecifications(segments[1], dataSetDefinitions, locations, regions, dr),
                Aggregation = ParseEnum<SeriesAggregationOptions>(segments[2]),
                RequestedColour = ParseEnum<Colours>(segments[3]),
                BinGranularity = ParseEnum<BinGranularities>(segments[4]),
                DisplayStyle = ParseEnum<SeriesDisplayStyle>(segments[5]),
                IsLocked = bool.Parse(segments[6]),
                SecondaryCalculation = ParseEnum<SecondaryCalculationOptions>(segments[7]),
                ShowTrendline = bool.Parse(segments[8]),
                Smoothing = ParseEnum<SeriesSmoothingOptions>(segments[9]),
                SmoothingWindow = int.Parse(segments[10]),
                Value = ParseEnum<SeriesValueOptions>(segments[11]),
                Year = ParseNullableShort(segments[12]),
                IsExpanded = bool.Parse(segments[13]),
                SeriesTransformation = ParseEnum<SeriesTransformations>(segments[14]),
                CustomTransformation = !string.IsNullOrEmpty(segments[15]) ? Uri.UnescapeDataString(segments[15]) : null,
                GroupingThreshold = ParseNullableFloat(segments[16]),
                DataAvailable = bool.Parse(segments[17]),
                MinimumDataResolution = (DataResolution?)ParseNullableEnum<DataResolution>(segments[18]),

                // The trend fields were added after links containing 19 segments were already being
                // shared, so they are read defensively rather than positionally - an older URL is
                // simply a series with the trend module switched off.
                ShowTrend = ParseOptionalBool(segments, 19),
                TrendPeriod = (TrendWindow?)ParseOptionalNullableEnum<TrendWindow>(segments, 20),
                TrendPredictionYears = TrendPredictionRange.Clamp(
                    ParseOptionalInt(segments, 21, TrendPredictionRange.Default)),

                // Added after links containing 22 segments were already being shared, so it's read
                // defensively too - an older URL simply defaults to Linear, today's only option.
                RegressionType = (TrendRegressionType?)ParseOptionalNullableEnum<TrendRegressionType>(segments, 22)
                    ?? TrendRegressionType.Linear,

                // Added after links containing 23 segments were already being shared; an older URL
                // simply has no fixed target year, i.e. today's duration-only behaviour.
                TrendPredictionTargetYear = ParseOptionalNullableInt(segments, 23),
            };
    }

    private static string? GetOptionalSegment(string[] segments, int index)
    {
        return index < segments.Length && !string.IsNullOrWhiteSpace(segments[index])
            ? segments[index]
            : null;
    }

    private static bool ParseOptionalBool(string[] segments, int index)
    {
        return GetOptionalSegment(segments, index) is { } value && bool.TryParse(value, out var parsed) && parsed;
    }

    private static int ParseOptionalInt(string[] segments, int index, int fallback)
    {
        return GetOptionalSegment(segments, index) is { } value && int.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    private static int? ParseOptionalNullableInt(string[] segments, int index)
    {
        return GetOptionalSegment(segments, index) is { } value && int.TryParse(value, out var parsed)
            ? parsed
            : null;
    }

    private static object? ParseOptionalNullableEnum<T>(string[] segments, int index)
        where T : struct, System.Enum
    {
        return GetOptionalSegment(segments, index) is { } value && Enum.TryParse<T>(value, out var parsed)
            ? parsed
            : null;
    }

    private static SourceSeriesSpecification[] ParseSourceSeriesSpecifications(string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IDictionary<Guid, Location>? locations, IEnumerable<Region> regions, DataResolution? dataResolution)
    {
        string[] segments = s.Split(SeparatorsByLevel[2]);

        return [.. segments.Select(x => ParseSourceSeriesSpecification(x, dataSetDefinitions, locations, regions, dataResolution))];
    }

    private static SourceSeriesSpecification ParseSourceSeriesSpecification(string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IDictionary<Guid, Location>? locations, IEnumerable<Region> regions, DataResolution? dataResolution)
    {
        string[] segments = s.Split(SeparatorsByLevel[3]);

        var dsd = dataSetDefinitions.Single(x => x.Id == Guid.Parse(segments[0]));
        var dt = (DataType?)ParseNullableEnum<DataType>(segments[1]);
        var da = (DataAdjustment?)ParseNullableEnum<DataAdjustment>(segments[2]);
        var dr = dataResolution;

        var dataMatches = new List<DataSubstitute>
        {
            new()
            {
                DataType = (DataType)dt,
                DataAdjustment = da,
                DataResolution = dr,
            },
        };
        if (dt == DataType.TempMax || dt == DataType.TempMean)
        {
            if (dr == DataResolution.Daily)
            {
                dataMatches = DataSubstitute.DailyMaxTemperatureDataMatches();
            }
            else if (da == DataAdjustment.Unadjusted)
            {
                dataMatches = DataSubstitute.UnadjustedTemperatureDataMatches();
            }
            else
            {
                dataMatches = DataSubstitute.AdjustedTemperatureDataMatches();
            }
        }
        else if (dt == DataType.TempMin && dr == DataResolution.Daily)
        {
            dataMatches = DataSubstitute.DailyMinTemperatureDataMatches();
        }

        GeographicalEntity? geographicalEntity = null;
        Guid? id = null;

        if (segments[3].Length > 0)
        {
            id = Guid.Parse(segments[3]);
            geographicalEntity = locations is not null && locations.ContainsKey(id.Value)
                                    ? locations[id.Value]
                                    : regions.Single(x => x.Id == id.Value);

            if (geographicalEntity == null)
            {
                throw new Exception($"A geographical entity (ID = {id}) has been specified in the source series specification that has not been found in the list of geographical entities.");
            }
        }

        // When dr is null (no minimum resolution was requested), more than one measurement
        // definition can match the same data type/adjustment (e.g. CO2 now has both Monthly and
        // Daily definitions). Prefer the coarsest match so existing URLs keep resolving to the
        // resolution they always have (Monthly) rather than throwing on an ambiguous match.
        var md = dsd.MeasurementDefinitions!
                .Where(x => x.DataAdjustment == da && x.DataType == dt && (dr == null || x.DataResolution == dr))
                .OrderBy(x => x.DataResolution)
                .FirstOrDefault();

        if (md == null)
        {
            foreach (var match in dataMatches)
            {
                var dsds = dataSetDefinitions.Where(x => x.LocationIds != null && (id is null || x.LocationIds.Contains(id.Value))
                                                      && x.MeasurementDefinitions!.Any(y => y.DataType == match.DataType && y.DataAdjustment == match.DataAdjustment && (match.DataResolution == null || y.DataResolution == match.DataResolution)))
                                         .ToList();

                if (dsds.Any())
                {
                    dsd = dsds.SingleOrDefault()!;
                    md = dsd.MeasurementDefinitions!.Single(x => x.DataType == match.DataType && x.DataAdjustment == match.DataAdjustment && (match.DataResolution == null || x.DataResolution == match.DataResolution));

                    break;
                }
            }
        }

        if (md == null)
        {
            throw new NullReferenceException($"Cannot find measurement definition in dataset {dsd.Id} with data type {dt} and data adjustment {da}.");
        }

        return
            new SourceSeriesSpecification
            {
                DataSetDefinition = dsd,
                MeasurementDefinition = md,
                LocationId = geographicalEntity!.Id,
                LocationName = geographicalEntity.Name,
            };
    }

    private static string BuildSourceSeriesSpecificationsUrlComponent(SourceSeriesSpecification sss)
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
                sss.LocationId);
    }

    private static string BuildSourceSeriesSpecificationsUrlComponent(SourceSeriesSpecification[] sss)
    {
        return
            string.Join(
                SeparatorsByLevel[2],
                sss.Select(BuildSourceSeriesSpecificationsUrlComponent));
    }

    private static string BuildChartSeriesUrlComponent(ChartSeriesDefinition csd)
    {
        return
            string.Join(
                SeparatorsByLevel[1],
                csd.SeriesDerivationType,
                BuildSourceSeriesSpecificationsUrlComponent(csd.SourceSeriesSpecifications!),
                csd.Aggregation,
                csd.RequestedColour,
                csd.BinGranularity,
                csd.DisplayStyle,
                csd.IsLocked,
                csd.SecondaryCalculation,
                csd.ShowTrendline,
                csd.Smoothing,
                csd.SmoothingWindow,
                csd.Value,
                csd.Year,
                csd.IsExpanded,
                csd.SeriesTransformation,
                Uri.EscapeDataString(csd.CustomTransformation ?? string.Empty),
                csd.GroupingThreshold,
                csd.DataAvailable,
                csd.MinimumDataResolution,
                csd.ShowTrend,
                csd.TrendPeriod,
                csd.TrendPredictionYears,
                csd.RegressionType,
                csd.TrendPredictionTargetYear);
    }
}
