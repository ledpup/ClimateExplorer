namespace ClimateExplorer.Web.UiLogic;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.Core.DataPreparation;
using static ClimateExplorer.Core.Enums;

public static class ChartSeriesListSerializer
{
    private static readonly char[] SeparatorsByLevel = [';', ',', '|', '*'];

    public static List<ChartSeriesDefinition> ParseChartSeriesDefinitionList(ILogger logger, string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IEnumerable<GeographicalEntity> geographicalEntities)
    {
        logger.LogInformation("ParseChartSeriesDefinitionList: " + s);

        string[] segments = s.Split(SeparatorsByLevel[0]);

        var seriesList =
            segments
            .Select(x => ParseChartSeriesUrlComponent(logger, x, dataSetDefinitions, geographicalEntities))
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

    private static ChartSeriesDefinition ParseChartSeriesUrlComponent(ILogger logger, string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IEnumerable<GeographicalEntity> geographicalEntities)
    {
        string[] segments = s.Split(SeparatorsByLevel[1]);

        var dr = (DataResolution?)ParseNullableEnum<DataResolution>(segments[17]);

        return
            new ChartSeriesDefinition()
            {
                SeriesDerivationType = ParseEnum<SeriesDerivationTypes>(segments[0]),
                SourceSeriesSpecifications = ParseSourceSeriesSpecifications(segments[1], dataSetDefinitions, geographicalEntities, dr),
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
                GroupingThreshold = ParseNullableFloat(segments[15]),
                DataAvailable = bool.Parse(segments[16]),
                MinimumDataResolution = (DataResolution?)ParseNullableEnum<DataResolution>(segments[17]),
            };
    }

    private static SourceSeriesSpecification[] ParseSourceSeriesSpecifications(string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IEnumerable<GeographicalEntity> geographicalEntities, DataResolution? dataResolution)
    {
        string[] segments = s.Split(SeparatorsByLevel[2]);

        return
            segments
            .Select(x => ParseSourceSeriesSpecification(x, dataSetDefinitions, geographicalEntities, dataResolution))
            .ToArray();
    }

    private static SourceSeriesSpecification ParseSourceSeriesSpecification(string s, IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, IEnumerable<GeographicalEntity> geographicalEntities, DataResolution? dataResolution)
    {
        string[] segments = s.Split(SeparatorsByLevel[3]);

        var dsd = dataSetDefinitions.Single(x => x.Id == Guid.Parse(segments[0]));
        var dt = (Core.Enums.DataType?)ParseNullableEnum<DataType>(segments[1]);
        var da = (DataAdjustment?)ParseNullableEnum<DataAdjustment>(segments[2]);
        var dr = dataResolution;

        var dataMatches = new List<DataSubstitute>
        {
            new ()
            {
                DataType = (Core.Enums.DataType)dt,
                DataAdjustment = da,
                DataResolution = dr,
            },
        };
        if (dt == Core.Enums.DataType.TempMax || dt == DataType.TempMean)
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
            geographicalEntity = geographicalEntities.SingleOrDefault(x => x.Id == id);

            if (geographicalEntity == null)
            {
                throw new Exception($"A geographical entity (ID = {id}) has been specified in the source series specification that has not been found in the list of geographical entities.");
            }
        }

        var md = dsd.MeasurementDefinitions!
                .SingleOrDefault(x => x.DataAdjustment == da && x.DataType == dt && (dr == null || x.DataResolution == dr));

        if (md == null)
        {
            foreach (var match in dataMatches)
            {
                var dsds = dataSetDefinitions.Where(x => x.LocationIds != null && x.LocationIds.Any(y => y == id)
                                                      && x.MeasurementDefinitions!.Any(y => y.DataType == match.DataType && y.DataAdjustment == match.DataAdjustment && (match.DataResolution == null || y.DataResolution == match.DataResolution)))
                                         .ToList();

                if (dsds.Any())
                {
                    dsd = dsds.SingleOrDefault() !;
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
                csd.GroupingThreshold,
                csd.DataAvailable,
                csd.MinimumDataResolution);
    }
}
