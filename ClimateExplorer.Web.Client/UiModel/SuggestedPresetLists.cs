using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Web.UiModel;

public static class SuggestedPresetLists
{
    public enum PresetTypes
    {
        Location,
        RegionalAndGlobal
    }

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

        var precipitation = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.Precipitation, null, throwIfNoMatch: false);
        var solarRadiation = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.SolarRadiation, null, throwIfNoMatch: false);
        var tempMin = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.TempMin, DataAdjustment.Adjusted, throwIfNoMatch: false);
        var tempMinUnadjusted = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.TempMin, DataAdjustment.Unadjusted, throwIfNoMatch: false);

        var nino34 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Earth), DataType.Nino34, null, throwIfNoMatch: true);
        var co2 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Atmosphere), DataType.CO2, null, throwIfNoMatch: true);
        var sunspot = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId("Sun"), DataType.SunspotNumber, null, throwIfNoMatch: true);

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
                        Year = null
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
                        Year = null
                    }
                ],
                Variants = [
                    new SuggestedChartPresetModelWithVariants()
                    {
                        Title = "ENSO + rainfall",
                        Description = "Monthly chart of the Nino 3.4 index and precipitation",
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
                                Year = null,
                                DisplayStyle = SeriesDisplayStyle.Line,
                                SeriesTransformation = SeriesTransformations.Identity,
                                RequestedColour = UiLogic.Colours.Green,
                            },
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Earth), nino34!),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYearAndMonth,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 3,
                                Value = SeriesValueOptions.Value,
                                Year = null,
                                DisplayStyle = SeriesDisplayStyle.Bar,
                                SeriesTransformation = SeriesTransformations.Identity,
                            },
                        ],
                    },
                    new SuggestedChartPresetModelWithVariants()
                    {
                        Title = "Days of rain",
                        Description = "Number of rainy days, ≥ 1mm and ≥ 10mm; 20-year smoothing",
                        ChartSeriesList =
                            [
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, precipitation!),
                                        Aggregation = SeriesAggregationOptions.Sum,
                                        BinGranularity = BinGranularities.ByYear,
                                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                                        SmoothingWindow = 20,
                                        Value = SeriesValueOptions.Value,
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Line,
                                        SeriesTransformation = SeriesTransformations.EqualOrAbove1,
                                        RequestedColour = UiLogic.Colours.Blue,
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
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Line,
                                        SeriesTransformation = SeriesTransformations.EqualOrAbove10,
                                        RequestedColour = UiLogic.Colours.Pink,
                                    }
                            ],
                        }
                ]
            }
        );

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
                            Year = null
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
                            Year = null,
                            RequestedColour = UiLogic.Colours.Black,
                        }
                ],
                Variants =
                [
                    new SuggestedChartPresetModel()
                    {
                        Title = "Temperature + sunspots",
                        Description = "Smoothed yearly average temperature and sunpot number (a proxy for solar radiation)",
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
                                Year = null
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
                                Year = null,
                                RequestedColour = UiLogic.Colours.Yellow,
                                GroupingThreshold = 0.1f,
                            }
                        ],
                    }
                ],
            });

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
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
                        Year = null,
                        DisplayStyle = SeriesDisplayStyle.Bar
                    }
                ],
                Variants =
                    [
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
                                        Year = null
                                    }
                                ]
                        },
                        new SuggestedChartPresetModelWithVariants()
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
                                    Smoothing = SeriesSmoothingOptions.None,
                                    SmoothingWindow = 5,
                                    Value = SeriesValueOptions.Value,
                                    Year = null
                                },
                                new ChartSeriesDefinition()
                                {
                                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempUnadjusted!),
                                    Aggregation = SeriesAggregationOptions.Mean,
                                    BinGranularity = BinGranularities.ByYear,
                                    ShowTrendline = false,
                                    Smoothing = SeriesSmoothingOptions.None,
                                    SmoothingWindow = 5,
                                    Value = SeriesValueOptions.Value,
                                    Year = null
                                }
                            ],
                        },
                    ]
            }
        );

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Days of extremes",
                Description = "Number of frosty days (≤ 2.2°C) and days 35°C or above; 20-year smoothing",
                ChartSeriesList =
                        [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, temperature!),
                                Aggregation = SeriesAggregationOptions.Sum,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                                Year = null,
                                DisplayStyle = SeriesDisplayStyle.Line,
                                SeriesTransformation = SeriesTransformations.EqualOrAbove35,
                            },
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMin!),
                                Aggregation = SeriesAggregationOptions.Sum,
                                BinGranularity = BinGranularities.ByYear,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 20,
                                Value = SeriesValueOptions.Value,
                                Year = null,
                                DisplayStyle = SeriesDisplayStyle.Line,
                                SeriesTransformation = SeriesTransformations.IsFrosty,
                            }
                        ],
                Variants =
                        [
                        new SuggestedChartPresetModelWithVariants()
                        {
                            Title = "First and last day of frost",
                            Description = "First and last day of the year that has temperature ≤ 2.2°C; 20-year smoothing",
                            ChartSeriesList =
                                [
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMin!),
                                        Aggregation = SeriesAggregationOptions.Minimum,
                                        BinGranularity = BinGranularities.ByYear,
                                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                                        SmoothingWindow = 20,
                                        Value = SeriesValueOptions.Value,
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Line,
                                        SeriesTransformation = SeriesTransformations.DayOfYearIfFrost,
                                    },
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMin!),
                                        Aggregation = SeriesAggregationOptions.Maximum,
                                        BinGranularity = BinGranularities.ByYear,
                                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                                        SmoothingWindow = 20,
                                        Value = SeriesValueOptions.Value,
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Line,
                                        SeriesTransformation = SeriesTransformations.DayOfYearIfFrost,
                                    }
                                ]
                        },
                    ]
            }
        );

        return suggestedPresets;
    }

    public static List<SuggestedChartPresetModelWithVariants> RegionalAndGlobalPresets(IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions)
    {
        var suggestedPresets = new List<SuggestedChartPresetModelWithVariants>();

        // This has been hacked in to be able to support a location group as a location ID. This needs to be refactored to support more than one location group
        // when we expand to other regions
        var tempMax = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId("Australia"), DataType.TempMax, DataAdjustment.Adjusted, throwIfNoMatch: false);
        var tempMaxUnadjusted = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId("Australia"), DataType.TempMax, DataAdjustment.Unadjusted, throwIfNoMatch: false);
        var precipitation = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId("Australia"), DataType.Precipitation, null, throwIfNoMatch: false);

        var co2 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Atmosphere), DataType.CO2, null, throwIfNoMatch: true);
        var ch4 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Atmosphere), DataType.CH4, null, throwIfNoMatch: true);
        var n2o = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Atmosphere), DataType.N2O, null, throwIfNoMatch: true);

        var northSeaIceExtent = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Arctic), DataType.SeaIceExtent, null, throwIfNoMatch: true);
        var southSeaIceExtent = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Antarctic), DataType.SeaIceExtent, null, throwIfNoMatch: true);
        var greenland = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Greenland), DataType.IceMeltArea, null, throwIfNoMatch: true);

        var nino34 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Earth), DataType.Nino34, null, throwIfNoMatch: true);
        var oni = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Earth), DataType.ONI, null, throwIfNoMatch: true);
        var soi = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Earth), DataType.SOI, null, throwIfNoMatch: true);
        var mei = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Earth), DataType.MEIv2, null, throwIfNoMatch: true);
        var iod = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Earth), DataType.IOD, null, throwIfNoMatch: true);

        var sunspots = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId("Sun"), DataType.SunspotNumber, null, throwIfNoMatch: true);
        var tsi = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId("Sun"), DataType.SolarRadiation, null, throwIfNoMatch: true);

        var globalTemp = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Earth), DataType.TempMean, null, throwIfNoMatch: true);
        var globalLandTemp = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId("Land"), DataType.TempMean, null, throwIfNoMatch: true);
        var globalOceanTemp = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId("Ocean"), DataType.TempMean, null, throwIfNoMatch: true);

        var northernTemp = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.NorthernHemi), DataType.TempMean, null, throwIfNoMatch: true);
        var southernTemp = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.SouthernHemi), DataType.TempMean, null, throwIfNoMatch: true);

        var arcticTemp = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Arctic), DataType.TempMean, null, throwIfNoMatch: true);
        var antarcticTemp = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Antarctic), DataType.TempMean, null, throwIfNoMatch: true);
        var r60S60NTemp = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.R60S60N), DataType.TempMean, null, throwIfNoMatch: true);

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Carbon dioxide annual change",
                Description = "Smoothed difference between current and previous year CO\u2082 maximums",
                ChartSeriesList =
                    [
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Atmosphere), co2!),
                                Aggregation = SeriesAggregationOptions.Maximum,
                                BinGranularity = BinGranularities.ByYear,
                                SecondaryCalculation = SecondaryCalculationOptions.AnnualChange,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 10,
                                Value = SeriesValueOptions.Value,
                                Year = null,
                                DisplayStyle = SeriesDisplayStyle.Line,
                                RequestedColour = UiLogic.Colours.Brown,
                            },
                    ],
                Variants =
                            [
                                new ()
                                {
                                    Title = "Carbon dioxide (CO\u2082)",
                                    Description = "Carbon dioxide records from the Mauna Loa Observatory since 1958. AKA The Keeling Curve",
                                    ChartSeriesList =
                                    [
                                            new ()
                                            {
                                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Atmosphere), co2!),
                                                Aggregation = SeriesAggregationOptions.Mean,
                                                BinGranularity = BinGranularities.ByYearAndMonth,
                                                Smoothing = SeriesSmoothingOptions.None,
                                                SmoothingWindow = 5,
                                                Value = SeriesValueOptions.Value,
                                                Year = null,
                                                DisplayStyle = SeriesDisplayStyle.Line,
                                                RequestedColour = UiLogic.Colours.Brown,
                                            },
                                    ],
                                },
                                new ()
                                {
                                    Title = "Methane (CH\u2084)",
                                    Description = "NOAA's Earth System Research Laboratory has measured methane since 1983",
                                    ChartSeriesList =
                                        [
                                            new ()
                                            {
                                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Atmosphere), ch4!),
                                                Aggregation = SeriesAggregationOptions.Mean,
                                                BinGranularity = BinGranularities.ByYearAndMonth,
                                                Smoothing = SeriesSmoothingOptions.None,
                                                SmoothingWindow = 5,
                                                Value = SeriesValueOptions.Value,
                                                Year = null,
                                                DisplayStyle = SeriesDisplayStyle.Line,
                                                RequestedColour = UiLogic.Colours.Brown,
                                            },
                                        ]
                                },
                                new ()
                                {
                                    Title = "Nitrous oxide (N\u2082O)",
                                    Description = "NOAA's Earth System Research Laboratory has measured nitrous oxide since 2001",
                                    ChartSeriesList =
                                        [
                                            new ()
                                            {
                                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Atmosphere), n2o!),
                                                Aggregation = SeriesAggregationOptions.Mean,
                                                BinGranularity = BinGranularities.ByYearAndMonth,
                                                Smoothing = SeriesSmoothingOptions.None,
                                                SmoothingWindow = 5,
                                                Value = SeriesValueOptions.Value,
                                                Year = null,
                                                DisplayStyle = SeriesDisplayStyle.Line,
                                                RequestedColour = UiLogic.Colours.Brown,
                                            }
                                        ]
                                }
                            ]
            }
            );


        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Global temperature anomaly",
                Description = "Global temperature anomalies of ocean, land and combined temperatures",
                ChartSeriesList =
                [
                    new ()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Earth), globalTemp!),
                        Aggregation = SeriesAggregationOptions.Mean,
                        BinGranularity = BinGranularities.ByYear,
                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                        SmoothingWindow = 20,
                        Value = SeriesValueOptions.Value,
                        Year = null,
                        DisplayStyle = SeriesDisplayStyle.Line,
                        RequestedColour = UiLogic.Colours.Red,
                    },
                    new ()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion("Land"), globalLandTemp!),
                        Aggregation = SeriesAggregationOptions.Mean,
                        BinGranularity = BinGranularities.ByYear,
                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                        SmoothingWindow = 20,
                        Value = SeriesValueOptions.Value,
                        Year = null,
                        DisplayStyle = SeriesDisplayStyle.Line,
                        RequestedColour = UiLogic.Colours.Green,
                    },
                    new ()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion("Ocean"), globalLandTemp!),
                        Aggregation = SeriesAggregationOptions.Mean,
                        BinGranularity = BinGranularities.ByYear,
                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                        SmoothingWindow = 20,
                        Value = SeriesValueOptions.Value,
                        Year = null,
                        DisplayStyle = SeriesDisplayStyle.Line,
                        RequestedColour = UiLogic.Colours.Blue,
                    },
                ],
                Variants = [
                    new ()
                    {
                        Title = "North vs south",
                        Description = "Combined land and ocean temperature anomalies for the Northern and Southern Hemisphere",
                        ChartSeriesList =
                            [
                                new ChartSeriesDefinition()
                                {
                                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.NorthernHemi), northernTemp!),
                                    Aggregation = SeriesAggregationOptions.Mean,
                                    BinGranularity = BinGranularities.ByYear,
                                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                                    SmoothingWindow = 20,
                                    Value = SeriesValueOptions.Value,
                                    Year = null,
                                    DisplayStyle = SeriesDisplayStyle.Line,
                                },
                                new ChartSeriesDefinition()
                                {
                                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.SouthernHemi), southernTemp!),
                                    Aggregation = SeriesAggregationOptions.Mean,
                                    BinGranularity = BinGranularities.ByYear,
                                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                                    SmoothingWindow = 20,
                                    Value = SeriesValueOptions.Value,
                                    Year = null,
                                    DisplayStyle = SeriesDisplayStyle.Line,
                                },
                            ]
                    },
                    new()
                    {
                        Title = "Arctic vs Antarctic",
                        Description = "Combined land and ocean temperatures for the Arctic, Antarctic and 60°S-60°N regions",
                        ChartSeriesList =
                            [
                                new ChartSeriesDefinition()
                                {
                                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Arctic), arcticTemp!),
                                    Aggregation = SeriesAggregationOptions.Mean,
                                    BinGranularity = BinGranularities.ByYear,
                                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                                    SmoothingWindow = 20,
                                    Value = SeriesValueOptions.Value,
                                    Year = null,
                                    DisplayStyle = SeriesDisplayStyle.Line,
                                },
                                new ChartSeriesDefinition()
                                {
                                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Antarctic), antarcticTemp!),
                                    Aggregation = SeriesAggregationOptions.Mean,
                                    BinGranularity = BinGranularities.ByYear,
                                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                                    SmoothingWindow = 20,
                                    Value = SeriesValueOptions.Value,
                                    Year = null,
                                    DisplayStyle = SeriesDisplayStyle.Line,
                                },
                                new ChartSeriesDefinition()
                                {
                                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.R60S60N), r60S60NTemp!),
                                    Aggregation = SeriesAggregationOptions.Mean,
                                    BinGranularity = BinGranularities.ByYear,
                                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                                    SmoothingWindow = 20,
                                    Value = SeriesValueOptions.Value,
                                    Year = null,
                                    DisplayStyle = SeriesDisplayStyle.Line,
                                },
                            ]
                    }
                ]
            }
        );


        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Sea ice extent",
                Description = "Antarctic and Arctic sea ice extent, measured in millions of square kilometres since 1979",
                ChartSeriesList =
                            [
                                new ChartSeriesDefinition()
                                {
                                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Antarctic), southSeaIceExtent!),
                                    Aggregation = SeriesAggregationOptions.Mean,
                                    BinGranularity = BinGranularities.ByYear,
                                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                                    SmoothingWindow = 10,
                                    Value = SeriesValueOptions.Value,
                                    Year = null,
                                    DisplayStyle = SeriesDisplayStyle.Line,
                                    RequestedColour = UiLogic.Colours.Purple,
                                    GroupingThreshold = 0.1f,
                                },
                                new ChartSeriesDefinition()
                                {
                                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Arctic), northSeaIceExtent!),
                                    Aggregation = SeriesAggregationOptions.Mean,
                                    BinGranularity = BinGranularities.ByYear,
                                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                                    SmoothingWindow = 10,
                                    Value = SeriesValueOptions.Value,
                                    Year = null,
                                    DisplayStyle = SeriesDisplayStyle.Line,
                                    RequestedColour = UiLogic.Colours.Orange,
                                    GroupingThreshold = 0.1f,
                                },
                            ],
                Variants =
                    [
                        new SuggestedChartPresetModel()
                        {
                            Title = "Greenland ice melt area",
                            Description = "Smoothed ice melt area, measured in square kilometres since 1979",
                            ChartSeriesList =
                                [
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Greenland), greenland!),
                                        Aggregation = SeriesAggregationOptions.Sum,
                                        BinGranularity = BinGranularities.ByYear,
                                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                                        SmoothingWindow = 10,
                                        Value = SeriesValueOptions.Value,
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Line,
                                        RequestedColour = UiLogic.Colours.Blue,
                                        GroupingThreshold = 0.1f,
                                    },
                                ]
                        },
                    ],
            }
        );

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Solar irradiation + sunspots",
                Description = "Total solar irradiance (from satelite data) compared with the number of sunspots",
                ChartSeriesList =
                [
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion("Sun"), tsi!),
                        Aggregation = SeriesAggregationOptions.Mean,
                        BinGranularity = BinGranularities.ByYear,
                        Smoothing = SeriesSmoothingOptions.None,
                        SmoothingWindow = 10,
                        Value = SeriesValueOptions.Value,
                        Year = null,
                        DisplayStyle = SeriesDisplayStyle.Line,
                        RequestedColour = UiLogic.Colours.Green,
                        GroupingThreshold = 0.1f,
                    },
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion("Sun"), sunspots!),
                        Aggregation = SeriesAggregationOptions.Mean,
                        BinGranularity = BinGranularities.ByYear,
                        Smoothing = SeriesSmoothingOptions.None,
                        SmoothingWindow = 10,
                        Value = SeriesValueOptions.Value,
                        Year = null,
                        DisplayStyle = SeriesDisplayStyle.Line,
                        RequestedColour = UiLogic.Colours.Yellow,
                        GroupingThreshold = 0.1f,
                    },
                ],
            }
        );

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Australian anomaly",
                Description = "Smoothed average of ACORN-SAT anomalies (excluding urban-influenced locations). Reference period is 1961-1990",
                ChartSeriesList =
                    [
                        new ChartSeriesDefinition()
                        {
                            SeriesDerivationType = SeriesDerivationTypes.AverageOfAnomaliesInRegion,
                            SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion("Australia"), tempMax!),
                            Aggregation = SeriesAggregationOptions.Mean,
                            BinGranularity = BinGranularities.ByYear,
                            Smoothing = SeriesSmoothingOptions.MovingAverage,
                            SmoothingWindow = 20,
                            Value = SeriesValueOptions.Anomaly,
                            Year = null,
                            DisplayStyle = SeriesDisplayStyle.Line,
                        },
                        new ChartSeriesDefinition()
                        {
                            SeriesDerivationType = SeriesDerivationTypes.AverageOfAnomaliesInRegion,
                            SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion("Australia"), precipitation!),
                            Aggregation = SeriesAggregationOptions.Sum,
                            BinGranularity = BinGranularities.ByYear,
                            Smoothing = SeriesSmoothingOptions.MovingAverage,
                            SmoothingWindow = 20,
                            Value = SeriesValueOptions.Anomaly,
                            Year = null,
                            DisplayStyle = SeriesDisplayStyle.Line,
                        },
                    ],
                Variants =
                    [
                        new SuggestedChartPresetModel()
                        {
                            Title = "Anomaly bar chart",
                            Description = "Australian temperature anomalies as a bar chart",
                            ChartSeriesList =
                                [
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.AverageOfAnomaliesInRegion,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion("Australia"), tempMax!),
                                        Aggregation = SeriesAggregationOptions.Mean,
                                        BinGranularity = BinGranularities.ByYear,
                                        Smoothing = SeriesSmoothingOptions.None,
                                        SmoothingWindow = 20,
                                        Value = SeriesValueOptions.Anomaly,
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Bar,
                                    },
                                ]
                        },
                        new SuggestedChartPresetModel()
                        {
                            Title = "Adjusted vs raw temperature",
                            Description = "Compare temperature values that have been adjusted for abnormalities with raw values",
                            ChartSeriesList =
                                [
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.AverageOfAnomaliesInRegion,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion("Australia"), tempMax!),
                                        Aggregation = SeriesAggregationOptions.Mean,
                                        BinGranularity = BinGranularities.ByYear,
                                        Smoothing = SeriesSmoothingOptions.None,
                                        SmoothingWindow = 20,
                                        Value = SeriesValueOptions.Anomaly,
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Line,
                                    },
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.AverageOfAnomaliesInRegion,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion("Australia"), tempMaxUnadjusted!),
                                        Aggregation = SeriesAggregationOptions.Mean,
                                        BinGranularity = BinGranularities.ByYear,
                                        Smoothing = SeriesSmoothingOptions.None,
                                        SmoothingWindow = 20,
                                        Value = SeriesValueOptions.Anomaly,
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Line,
                                    },
                                ]
                        },
                    ],
            });

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "ENSO + IOD",
                Description = "The El Niño Southern Oscillation (ENSO) and Indian Ocean Dipole (IOD) are sea surface temperature anomalies",
                ChartSeriesList =
                            [
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Earth), nino34!),
                                        Aggregation = SeriesAggregationOptions.Mean,
                                        BinGranularity = BinGranularities.ByYear,
                                        Smoothing = SeriesSmoothingOptions.None,
                                        SmoothingWindow = 10,
                                        Value = SeriesValueOptions.Value,
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Line,
                                        RequestedColour = UiLogic.Colours.Blue,
                                        GroupingThreshold = 0.1f,
                                    },
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Earth), iod!),
                                        Aggregation = SeriesAggregationOptions.Mean,
                                        BinGranularity = BinGranularities.ByYear,
                                        Smoothing = SeriesSmoothingOptions.None,
                                        SmoothingWindow = 10,
                                        Value = SeriesValueOptions.Value,
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Line,
                                        RequestedColour = UiLogic.Colours.Orange,
                                        GroupingThreshold = 0.1f,
                                    },
                            ],
                Variants =
                                [
                                    new SuggestedChartPresetModel()
                                    {
                                        Title = "All ENSO",
                                        Description = "Nino 3.4, ONI, an inverted SOI, and MEI v2",
                                        ChartSeriesList =
                                            [
                                                    new ChartSeriesDefinition()
                                                    {
                                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Earth), nino34!),
                                                        Aggregation = SeriesAggregationOptions.Mean,
                                                        BinGranularity = BinGranularities.ByYearAndMonth,
                                                        Smoothing = SeriesSmoothingOptions.None,
                                                        SmoothingWindow = 10,
                                                        Value = SeriesValueOptions.Value,
                                                        Year = null,
                                                        DisplayStyle = SeriesDisplayStyle.Line,
                                                        RequestedColour = UiLogic.Colours.Blue,
                                                        GroupingThreshold = 0.1f,
                                                    },
                                                    new ChartSeriesDefinition()
                                                    {
                                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Earth), oni!),
                                                        Aggregation = SeriesAggregationOptions.Mean,
                                                        BinGranularity = BinGranularities.ByYearAndMonth,
                                                        Smoothing = SeriesSmoothingOptions.None,
                                                        SmoothingWindow = 10,
                                                        Value = SeriesValueOptions.Value,
                                                        Year = null,
                                                        DisplayStyle = SeriesDisplayStyle.Line,
                                                        GroupingThreshold = 0.1f,
                                                    },
                                                    new ChartSeriesDefinition()
                                                    {
                                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Earth), soi!),
                                                        Aggregation = SeriesAggregationOptions.Mean,
                                                        BinGranularity = BinGranularities.ByYearAndMonth,
                                                        Smoothing = SeriesSmoothingOptions.None,
                                                        SmoothingWindow = 10,
                                                        Value = SeriesValueOptions.Value,
                                                        Year = null,
                                                        DisplayStyle = SeriesDisplayStyle.Line,
                                                        GroupingThreshold = 0.1f,
                                                        SeriesTransformation = SeriesTransformations.Negate,
                                                    },
                                                    new ChartSeriesDefinition()
                                                    {
                                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Earth), mei!),
                                                        Aggregation = SeriesAggregationOptions.Mean,
                                                        BinGranularity = BinGranularities.ByYearAndMonth,
                                                        Smoothing = SeriesSmoothingOptions.None,
                                                        SmoothingWindow = 10,
                                                        Value = SeriesValueOptions.Value,
                                                        Year = null,
                                                        DisplayStyle = SeriesDisplayStyle.Line,
                                                        GroupingThreshold = 0.1f,
                                                    },
                                            ]
                                    },
                                ],
            }
        );

        return suggestedPresets;
    }
}