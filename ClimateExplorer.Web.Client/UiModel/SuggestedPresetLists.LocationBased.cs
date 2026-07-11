namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using static ClimateExplorer.Core.Enums;

public static partial class SuggestedPresetLists
{
    public static List<SuggestedChartPresetModelWithVariants> LocationBasedPresets(IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, Location location)
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
        var solarRadiation = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.SolarRadiation, null, throwIfNoMatch: false);

        var nino34 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Ocean), DataType.Nino34, null, throwIfNoMatch: true);
        var co2 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Atmosphere), DataType.CO2, null, throwIfNoMatch: true);
        var sunspot = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId("Sun"), DataType.SunspotNumber, null, throwIfNoMatch: true);
        var transmission = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Atmosphere), DataType.ApparentTransmission, null, throwIfNoMatch: true);

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
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
                Variants = [
                    new SuggestedChartPresetModel
                    {
                        Title = "Temperature anomaly",
                        Description = "Yearly average temperatures relative to the average of the whole dataset",
                        ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, temperature!),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.None,
                                SmoothingWindow = 5,
                                Value = SeriesValueOptions.Anomaly,
                                DisplayStyle = SeriesDisplayStyle.Bar,
                            },
                        ],
                    },
                    new SuggestedChartPresetModel()
                    {
                        Title = "Temperature with trendline",
                        Description = "Yearly view of average temperature with a straight line fit to the data (the trendline)",
                        ChartSeriesList =
                            [
                                new ChartSeriesDefinition()
                                {
                                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, temperature!),
                                    Aggregation = SeriesAggregationOptions.Mean,
                                    BinGranularity = BinGranularities.ByYear,
                                    ShowTrendline = true,
                                    Smoothing = SeriesSmoothingOptions.None,
                                    SmoothingWindow = 5,
                                    Value = SeriesValueOptions.Value,
                                    DisplayStyle = SeriesDisplayStyle.Scatter,
                                },
                            ],
                    },
                    new SuggestedChartPresetModel()
                    {
                        Title = "Adjusted vs raw temperature",
                        Description = "Compare temperature values that have been adjusted for abnormalities with raw values",
                        ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempAdjusted!),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYear,
                                ShowTrendline = false,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                            },
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempUnadjusted!),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYear,
                                ShowTrendline = false,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                                RequestedColour = UiLogic.Colours.Green,
                            },
                        ],
                    },
                ],
            });

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Temperature + CO₂",
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
                    }

                ],
                Variants =
                [
                    new SuggestedChartPresetModel()
                    {
                        Title = "Temperature + sunspots",
                        Description = "Smoothed yearly average temperature and sunspot number (a proxy for solar radiation)",
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
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion("Sun"), sunspot!),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.None,
                                SmoothingWindow = 5,
                                Value = SeriesValueOptions.Value,
                                GroupingThreshold = 0.1f,
                                RequestedColour = UiLogic.Colours.Yellow,
                            }

                        ],
                    },
                    new SuggestedChartPresetModel()
                    {
                        Title = "Temperature + transmission",
                        Description = "Yearly average temperature and apparent atmospheric transmission from Mauna Loa. (May show the effect of volcanic erruptions on temperatures.)",
                        ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, temperature!),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.None,
                                SmoothingWindow = 5,
                                Value = SeriesValueOptions.Value,
                            },
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Atmosphere), transmission!),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.None,
                                SmoothingWindow = 5,
                                Value = SeriesValueOptions.Value,
                                GroupingThreshold = 0.1f,
                                RequestedColour = UiLogic.Colours.Yellow,
                            }

                        ],
                    },
                ],
            });

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
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
                Variants =
                [
                    new SuggestedChartPresetModel
                    {
                        Title = "Days of rain",
                        Description = "Rainy days ≥ 1mm and ≥ 10mm; 20-year smoothing",
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
                                CustomTransformation = "x >= 1",
                                MinimumDataResolution = DataResolution.Daily,
                                RequestedColour = UiLogic.Colours.Blue,
                            },
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
                                CustomTransformation = "x >= 10",
                                MinimumDataResolution = DataResolution.Daily,
                                RequestedColour = UiLogic.Colours.Pink,
                            },
                        ],
                    },
                    new SuggestedChartPresetModel()
                    {
                        Title = "ENSO + precipitation",
                        Description = "Monthly chart of the Nino 3.4 index and precipitation",
                        StartYear = 2000,
                        ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, precipitation!),
                                Aggregation = SeriesAggregationOptions.Sum,
                                BinGranularity = BinGranularities.ByYearAndMonth,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 3,
                                Value = SeriesValueOptions.Value,
                                DisplayStyle = SeriesDisplayStyle.Line,
                                SeriesTransformation = SeriesTransformations.Identity,
                                RequestedColour = UiLogic.Colours.Green,
                            },
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Ocean), nino34!),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYearAndMonth,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 3,
                                Value = SeriesValueOptions.Value,
                                DisplayStyle = SeriesDisplayStyle.Bar,
                                SeriesTransformation = SeriesTransformations.Identity,
                            },
                        ],
                    },
                ],
            });

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Days of extremes",
                Description = "Number of frosty days (≤ 2.2°C) and days 35°C or above",
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
                        RequestedColour = UiLogic.Colours.Green,
                    },
                ],
                Variants =
                [
                    new SuggestedChartPresetModel()
                    {
                        Title = "Hot nights",
                        Description = "Number of days when the minimum temperature is 25°C or above",
                        ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, dailyTempMin!),
                                Aggregation = SeriesAggregationOptions.Sum,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.None,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                                DisplayStyle = SeriesDisplayStyle.Bar,
                                SeriesTransformation = SeriesTransformations.Custom,
                                CustomTransformation = "x >= 25",
                                MinimumDataResolution = DataResolution.Daily,
                            },
                        ],
                    },
                    new SuggestedChartPresetModel()
                    {
                        Title = "First and last day of frost",
                        Description = "First and last day of the year that has temperature ≤ 2.2°C",
                        ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, dailyTempMin!),
                                Aggregation = SeriesAggregationOptions.Minimum,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                                DisplayStyle = SeriesDisplayStyle.Line,
                                SeriesTransformation = SeriesTransformations.DayOfYearIfFrost,
                                MinimumDataResolution = DataResolution.Daily,
                                RequestedColour = UiLogic.Colours.Grey,
                            },
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, dailyTempMin!),
                                Aggregation = SeriesAggregationOptions.Maximum,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                                DisplayStyle = SeriesDisplayStyle.Line,
                                SeriesTransformation = SeriesTransformations.DayOfYearIfFrost,
                                MinimumDataResolution = DataResolution.Daily,
                                RequestedColour = UiLogic.Colours.Purple,
                            },
                        ],
                    },
                    new SuggestedChartPresetModel()
                    {
                        Title = "Maximum temperatures",
                        Description = "Highest maximum daily temperature for each year",
                        ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, dailyTempMax!),
                                Aggregation = SeriesAggregationOptions.Maximum,
                                BinGranularity = BinGranularities.ByYear,
                                ShowTrendline = false,
                                Smoothing = SeriesSmoothingOptions.None,
                                SmoothingWindow = 5,
                                Value = SeriesValueOptions.Value,
                            },
                        ],
                    },
                ],
            });

        return suggestedPresets;
    }
}
