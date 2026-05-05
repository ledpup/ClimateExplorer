namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using static ClimateExplorer.Core.Enums;

public static partial class SuggestedPresetLists
{
    public static List<SuggestedChartPresetModelWithVariants> LocationBasedPresetsMobile(IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, Location location)
    {
        var suggestedPresets = new List<SuggestedChartPresetModelWithVariants>();

        if (location == null)
        {
            throw new Exception();
        }

        var temperature = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataSubstitute.StandardTemperatureDataMatches(), throwIfNoMatch: false);
        var tempAdjusted = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataSubstitute.AdjustedTemperatureDataMatches(), throwIfNoMatch: false);
        var tempUnadjusted = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataSubstitute.UnadjustedTemperatureDataMatches(), throwIfNoMatch: false);

        var dailyTempMax = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataSubstitute.DailyMaxTemperatureDataMatches(), throwIfNoMatch: false);
        var dailyTempMin = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataSubstitute.DailyMinTemperatureDataMatches(), throwIfNoMatch: false);

        var precipitation = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.Precipitation, null, throwIfNoMatch: false);
        var ds = new DataSubstitute { DataType = DataType.Precipitation, DataResolution = DataResolution.Daily };
        var dailyPrecipitation = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, new List<DataSubstitute> { ds }, throwIfNoMatch: false);

        var co2 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Atmosphere), DataType.CO2, null, throwIfNoMatch: true);

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Temperature",
                Description = "Smoothed yearly average temperature",
                ChartSeriesList =
                [
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, temperature!),
                        Aggregation = SeriesAggregationOptions.Mean,
                        BinGranularity = BinGranularities.ByYear,
                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                        SmoothingWindow = 20,
                        Value = SeriesValueOptions.Value,
                    },
                ],
                Variants =
                [
                    new()
                    {
                        Title = "Temperature + precipitation",
                        Description = "Smoothed yearly average temperature and precipitation",
                        ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, temperature!),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                            },
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, precipitation!),
                                Aggregation = SeriesAggregationOptions.Sum,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                            },
                        ],
                    },
                    new()
                    {
                        Title = "Temperature + CO\u2082",
                        Description = "Smoothed yearly average temperature and carbon dioxide",
                        ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, temperature!),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 10,
                                Value = SeriesValueOptions.Value,
                            },
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Atmosphere), co2!),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.None,
                                SmoothingWindow = 5,
                                Value = SeriesValueOptions.Value,
                                RequestedColour = UiLogic.Colours.Black,
                            },
                        ],
                    },
                    new()
                    {
                        Title = "Dry days",
                        Description = "Number of days without rain; 20-year smoothing",
                        ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, dailyPrecipitation!),
                                Aggregation = SeriesAggregationOptions.Sum,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                                DisplayStyle = SeriesDisplayStyle.Line,
                                SeriesTransformation = SeriesTransformations.Custom,
                                CustomTransformation = "x == 0",
                                MinimumDataResolution = DataResolution.Daily,
                                RequestedColour = UiLogic.Colours.Brown,
                            },
                        ],
                    },
                    new()
                    {
                        Title = "Days of extremes",
                        Description = "Number of frosty days (\u2264 2.2\u00b0C) and days 35\u00b0C or above",
                        ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, dailyTempMax!),
                                Aggregation = SeriesAggregationOptions.Sum,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                                DisplayStyle = SeriesDisplayStyle.Line,
                                SeriesTransformation = SeriesTransformations.Custom,
                                CustomTransformation = "x >= 35",
                                MinimumDataResolution = DataResolution.Daily,
                            },
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, dailyTempMin!),
                                Aggregation = SeriesAggregationOptions.Sum,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                                DisplayStyle = SeriesDisplayStyle.Line,
                                SeriesTransformation = SeriesTransformations.Custom,
                                CustomTransformation = "x <= 2.2",
                                MinimumDataResolution = DataResolution.Daily,
                            },
                        ],
                    },
                ],
            });

        return suggestedPresets;
    }
}
