using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.UiModel;
using ClimateExplorer.Core.DataPreparation;
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
        if (string.IsNullOrWhiteSpace(s)) return null!;

        return (T)Enum.Parse(typeof(T), s);
    }

    static T ParseEnum<T>(string s) where T : notnull, System.Enum
    {
        return (T)Enum.Parse(typeof(T), s);
    }

    static float? ParseNullableFloat(string s)
    {
        if (String.IsNullOrWhiteSpace(s)) return null;

        if (float.TryParse(s, out float val))
        {
            return val;
        }

        throw new Exception($"Failed to parse '{s}'");
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

        logger.LogInformation($"Returning seriesList with {seriesList.Count} elements");

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
        var dt = (Core.Enums.DataType?)ParseNullableEnum<Core.Enums.DataType>(segments[1]);
        var da = (DataAdjustment?)ParseNullableEnum<DataAdjustment>(segments[2]);

        var dataMatches = new List<DataSubstitute>
        {
            new DataSubstitute
            {
                DataType = (Core.Enums.DataType)dt,
                DataAdjustment = da,
            }
        };
        if (dt == Core.Enums.DataType.TempMax || dt == Core.Enums.DataType.TempMean)
        {
            if (da == DataAdjustment.Unadjusted)
            {
                dataMatches = DataSubstitute.UnadjustedTemperatureDataMatches();
            }
            else
            {
                dataMatches = DataSubstitute.AdjustedTemperatureDataMatches();
            }
        }

        Location? l = null;
        Guid? locationId = null;

        if (segments[3].Length > 0)
        {
            locationId = Guid.Parse(segments[3]);
            l = locations.SingleOrDefault(x => x.Id == locationId);

            if (l == null)
            {
                throw new Exception($"A location (ID = {locationId}) has been specified in the source series specification that has not been found in the list of locations.");
            }
        }

        var md = dsd.MeasurementDefinitions!
                .SingleOrDefault(x => x.DataAdjustment == da && x.DataType == dt);

        if (md == null)
        {
            foreach (var match in dataMatches)
            {
                var dsds = dataSetDefinitions.Where(x => (locationId == null
                                                            || (x.LocationIds != null && x.LocationIds.Any(y => y == locationId))
                                                         )
                                                 && x.MeasurementDefinitions!.Any(y => y.DataType == match.DataType && y.DataAdjustment == match.DataAdjustment))
                                         .ToList();

                if (dsds.Any())
                {
                    dsd = dsds.SingleOrDefault()!;
                    md = dsd.MeasurementDefinitions!.Single(x => x.DataType == match.DataType && x.DataAdjustment == match.DataAdjustment);

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
                csd.DataAvailable
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
