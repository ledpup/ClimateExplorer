using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.Shared;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Visualiser.UiModel;

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

        var tempMax = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.TempMax, DataAdjustment.Adjusted, true, throwIfNoMatch: false);
        var tempMaxUnadjusted = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.TempMax, DataAdjustment.Unadjusted, false, throwIfNoMatch: false);
        var tempMaxUnadjustedOrUnspecified = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.TempMax, DataAdjustment.Unadjusted, true, throwIfNoMatch: false);
        var rainfall = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.Rainfall, null, throwIfNoMatch: false);
        var solarRadiation = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.SolarRadiation, null, throwIfNoMatch: false);
        var tempMin = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.TempMin, DataAdjustment.Adjusted, true, throwIfNoMatch: false);
        var tempMinUnadjusted = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, location.Id, DataType.TempMin, DataAdjustment.Unadjusted, false, throwIfNoMatch: false);

        var nino34 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.Nino34, null, false, throwIfNoMatch: true);

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Temperature + rainfall",
                Description = "Smoothed maximum daily temperature and rainfall",
                ChartSeriesList =
                new List<ChartSeriesDefinition>()
                {
                    new ChartSeriesDefinition()
                    {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMax),
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
                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, rainfall),
                        Aggregation = SeriesAggregationOptions.Sum,
                        BinGranularity = BinGranularities.ByYear,
                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                        SmoothingWindow = 20,
                        Value = SeriesValueOptions.Value,
                        Year = null
                    }
                },
                Variants = new List<SuggestedChartPresetModel> {
                    new SuggestedChartPresetModelWithVariants()
                    {
                        Title = "ENSO + rainfall",
                        Description = "Monthly chart of the Nino 3.4 index and rainfall",
                        ChartSeriesList =
                        new List<ChartSeriesDefinition>()
                        {
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, rainfall),
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
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, nino34),
                                Aggregation = SeriesAggregationOptions.Mean,
                                BinGranularity = BinGranularities.ByYearAndMonth,
                                Smoothing = SeriesSmoothingOptions.MovingAverage,
                                SmoothingWindow = 3,
                                Value = SeriesValueOptions.Value,
                                Year = null,
                                DisplayStyle = SeriesDisplayStyle.Bar,
                                SeriesTransformation = SeriesTransformations.Identity,
                            },
                        },
                    },
                    new SuggestedChartPresetModelWithVariants()
                    {
                        Title = "Days of rain",
                        Description = "Number of rainy days, ≥ 1mm and ≥ 10mm; 20-year smoothing",
                        ChartSeriesList =
                            new List<ChartSeriesDefinition>()
                            {
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, rainfall),
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
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, rainfall),
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
                            },
                        }
                }
            }
        );

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Temperature anomaly",
                Description = "Yearly average maximum daily temperatures relative to the average of the whole dataset",
                ChartSeriesList =
                    new List<ChartSeriesDefinition>()
                    {
                        new ChartSeriesDefinition()
                        {
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMax),
                        Aggregation = SeriesAggregationOptions.Mean,
                        BinGranularity = BinGranularities.ByYear,
                        Smoothing = SeriesSmoothingOptions.None,
                        SmoothingWindow = 5,
                        Value = SeriesValueOptions.Anomaly,
                        Year = null,
                        DisplayStyle = SeriesDisplayStyle.Bar
                    }
                },
                Variants =
                    new List<SuggestedChartPresetModel>()
                    {
                        new SuggestedChartPresetModel()
                        {
                            Title = "Temperature with trendline",
                            Description = "Yearly view of average maximum daily temperature with a straight line, fit to the data",
                            ChartSeriesList =
                                new List<ChartSeriesDefinition>()
                                {
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMax),
                                        Aggregation = SeriesAggregationOptions.Mean,
                                        BinGranularity = BinGranularities.ByYear,
                                        ShowTrendline = true,
                                        Smoothing = SeriesSmoothingOptions.None,
                                        SmoothingWindow = 5,
                                        Value = SeriesValueOptions.Value,
                                        Year = null
                                    }
                                }
                        },
                        new SuggestedChartPresetModelWithVariants()
                        {
                            Title = "Adjusted vs raw temperature",
                            Description = "Compare maximum temperature values that have been adjusted for abnormalities with raw values",
                            ChartSeriesList =
                            new List<ChartSeriesDefinition>()
                            {
                                new ChartSeriesDefinition()
                                {
                                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMax),
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
                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMaxUnadjusted),
                                    Aggregation = SeriesAggregationOptions.Mean,
                                    BinGranularity = BinGranularities.ByYear,
                                    ShowTrendline = false,
                                    Smoothing = SeriesSmoothingOptions.None,
                                    SmoothingWindow = 5,
                                    Value = SeriesValueOptions.Value,
                                    Year = null
                                }
                            },
                        },
                        new SuggestedChartPresetModel()
                        {
                            Title = "Temperature + solar radiation",
                            Description = "Shows yearly solar radiation and maximum temperature averages",
                            ChartSeriesList =
                                new List<ChartSeriesDefinition>()
                                {
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMaxUnadjustedOrUnspecified),
                                        Aggregation = SeriesAggregationOptions.Mean,
                                        BinGranularity = BinGranularities.ByYearAndMonth,
                                        ShowTrendline = false,
                                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                                        SmoothingWindow = 3,
                                        Value = SeriesValueOptions.Value,
                                        Year = null
                                    },
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, solarRadiation),
                                        Aggregation = SeriesAggregationOptions.Mean,
                                        BinGranularity = BinGranularities.ByYearAndMonth,
                                        ShowTrendline = false,
                                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                                        SmoothingWindow = 3,
                                        Value = SeriesValueOptions.Value,
                                        Year = null,
                                        RequestedColour = UiLogic.Colours.Green,
                                    }
                                }
                        },
                    }
            }
        );

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Days of extremes",
                Description = "Number of frosty days (≤ 2.2°C) and days 35°C or above; 20-year smoothing",
                ChartSeriesList =
                        new List<ChartSeriesDefinition>()
                        {
                            new ChartSeriesDefinition()
                            {
                            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                            SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMax),
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
                            SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMin),
                            Aggregation = SeriesAggregationOptions.Sum,
                            BinGranularity = BinGranularities.ByYear,
                            Smoothing = SeriesSmoothingOptions.MovingAverage,
                            SmoothingWindow = 20,
                            Value = SeriesValueOptions.Value,
                            Year = null,
                            DisplayStyle = SeriesDisplayStyle.Line,
                            SeriesTransformation = SeriesTransformations.IsFrosty,
                        }
                        },
                Variants = new List<SuggestedChartPresetModel>()
                        {
                        new SuggestedChartPresetModelWithVariants()
                        {
                            Title = "First and last day of frost",
                            Description = "First and last day of the year that has temperature ≤ 2.2°C; 20-year smoothing",
                            ChartSeriesList =
                                new List<ChartSeriesDefinition>()
                                {
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMin),
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
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMin),
                                        Aggregation = SeriesAggregationOptions.Maximum,
                                        BinGranularity = BinGranularities.ByYear,
                                        Smoothing = SeriesSmoothingOptions.MovingAverage,
                                        SmoothingWindow = 20,
                                        Value = SeriesValueOptions.Value,
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Line,
                                        SeriesTransformation = SeriesTransformations.DayOfYearIfFrost,
                                    }
                                }
                        },
                    }
            }
        );

        return suggestedPresets;
    }

    public static List<SuggestedChartPresetModelWithVariants> RegionalAndGlobalPresets(IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions)
    {
        var suggestedPresets = new List<SuggestedChartPresetModelWithVariants>();

        var co2 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.CO2, null, false, throwIfNoMatch: true);
        var ch4 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.CH4, null, false, throwIfNoMatch: true);
        var n2o = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.N2O, null, false, throwIfNoMatch: true);

        var northSeaIceExtent = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.NorthSeaIce, null, false, throwIfNoMatch: true);
        var southSeaIceExtent = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.SouthSeaIce, null, false, throwIfNoMatch: true);
        var greenland = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.GreenlandIceMelt, null, false, throwIfNoMatch: true);

        var nino34 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.Nino34, null, false, throwIfNoMatch: true);
        var oni = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.ONI, null, false, throwIfNoMatch: true);
        var soi = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.SOI, null, false, throwIfNoMatch: true);
        var mei = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.MEIv2, null, false, throwIfNoMatch: true);
        var iod = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, null, DataType.IOD, null, false, throwIfNoMatch: true);

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Carbon dioxide annual change",
                Description = "Smoothed difference between current and previous year CO\u2082 maximums",
                ChartSeriesList =
                    new List<ChartSeriesDefinition>()
                    {
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, co2),
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
                    },
                Variants =
                            new List<SuggestedChartPresetModel>()
                            {
                                new SuggestedChartPresetModel()
                                {
                                    Title = "Carbon dioxide (CO\u2082)",
                                    Description = "Carbon dioxide records from the Mauna Loa Observatory since 1958. AKA The Keeling Curve",
                                    ChartSeriesList =
                                    new List<ChartSeriesDefinition>()
                                    {
                                            new ChartSeriesDefinition()
                                            {
                                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, co2),
                                                Aggregation = SeriesAggregationOptions.Mean,
                                                BinGranularity = BinGranularities.ByYearAndMonth,
                                                Smoothing = SeriesSmoothingOptions.None,
                                                SmoothingWindow = 5,
                                                Value = SeriesValueOptions.Value,
                                                Year = null,
                                                DisplayStyle = SeriesDisplayStyle.Line,
                                                RequestedColour = UiLogic.Colours.Brown,
                                            },
                                    },
                                },
                                new SuggestedChartPresetModel()
                                {
                                    Title = "Methane (CH\u2084)",
                                    Description = "NOAA's Earth System Research Laboratory has measured methane since 1983",
                                    ChartSeriesList =
                                        new List<ChartSeriesDefinition>()
                                        {
                                            new ChartSeriesDefinition()
                                            {
                                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, ch4),
                                                Aggregation = SeriesAggregationOptions.Mean,
                                                BinGranularity = BinGranularities.ByYearAndMonth,
                                                Smoothing = SeriesSmoothingOptions.None,
                                                SmoothingWindow = 5,
                                                Value = SeriesValueOptions.Value,
                                                Year = null,
                                                DisplayStyle = SeriesDisplayStyle.Line,
                                                RequestedColour = UiLogic.Colours.Brown,
                                            },
                                        }
                                },
                                new SuggestedChartPresetModel()
                                {
                                    Title = "Nitrous oxide (N\u2082O)",
                                    Description = "NOAA's Earth System Research Laboratory has measured nitrous oxide since 2001",
                                    ChartSeriesList =
                                        new List<ChartSeriesDefinition>()
                                        {
                                            new ChartSeriesDefinition()
                                            {
                                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, n2o),
                                                Aggregation = SeriesAggregationOptions.Mean,
                                                BinGranularity = BinGranularities.ByYearAndMonth,
                                                Smoothing = SeriesSmoothingOptions.None,
                                                SmoothingWindow = 5,
                                                Value = SeriesValueOptions.Value,
                                                Year = null,
                                                DisplayStyle = SeriesDisplayStyle.Line,
                                                RequestedColour = UiLogic.Colours.Brown,
                                            }
                                        }
                                }
                            }
            }
            );

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "Sea ice extent",
                Description = "Antarctic and Arctic sea ice extent, measured in millions of square kilometres since 1979",
                ChartSeriesList =
                            new List<ChartSeriesDefinition>()
                            {
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, southSeaIceExtent),
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
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, northSeaIceExtent),
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
                            },
                Variants =
                                new List<SuggestedChartPresetModel>()
                                {
                                    new SuggestedChartPresetModel()
                                    {
                                        Title = "Greenland ice melt area",
                                        Description = "Smoothed ice melt area, measured in square kilometres since 1979",
                                        ChartSeriesList =
                                            new List<ChartSeriesDefinition>()
                                            {
                                                new ChartSeriesDefinition()
                                                {
                                                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, greenland),
                                                    Aggregation = SeriesAggregationOptions.Sum,
                                                    BinGranularity = BinGranularities.ByYear,
                                                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                                                    SmoothingWindow = 10,
                                                    Value = SeriesValueOptions.Value,
                                                    Year = null,
                                                    DisplayStyle = SeriesDisplayStyle.Line,
                                                    RequestedColour = UiLogic.Colours.Blue,
                                                },
                                            }
                                    },
                                },
            }
        );

        suggestedPresets.Add(
            new SuggestedChartPresetModelWithVariants()
            {
                Title = "ENSO + IOD",
                Description = "The El Niño Southern Oscillation (ENSO) and Indian Ocean Dipole (IOD) are sea surface temperature anomalies.",
                ChartSeriesList =
                            new List<ChartSeriesDefinition>()
                            {
                                    new ChartSeriesDefinition()
                                    {
                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, nino34),
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
                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, iod),
                                        Aggregation = SeriesAggregationOptions.Mean,
                                        BinGranularity = BinGranularities.ByYearAndMonth,
                                        Smoothing = SeriesSmoothingOptions.None,
                                        SmoothingWindow = 10,
                                        Value = SeriesValueOptions.Value,
                                        Year = null,
                                        DisplayStyle = SeriesDisplayStyle.Line,
                                        RequestedColour = UiLogic.Colours.Orange,
                                        GroupingThreshold = 0.1f,
                                    },
                            },
                Variants = new List<SuggestedChartPresetModel>()
                                {
                                    new SuggestedChartPresetModel()
                                    {
                                        Title = "All ENSO",
                                        Description = "Nino 3.4, ONI, an inverted SOI, and MEI v2",
                                        ChartSeriesList =
                                            new List<ChartSeriesDefinition>()
                                            {
                                                    new ChartSeriesDefinition()
                                                    {
                                                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, nino34),
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
                                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, oni),
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
                                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, soi),
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
                                                        SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, mei),
                                                        Aggregation = SeriesAggregationOptions.Mean,
                                                        BinGranularity = BinGranularities.ByYearAndMonth,
                                                        Smoothing = SeriesSmoothingOptions.None,
                                                        SmoothingWindow = 10,
                                                        Value = SeriesValueOptions.Value,
                                                        Year = null,
                                                        DisplayStyle = SeriesDisplayStyle.Line,
                                                        GroupingThreshold = 0.1f,
                                                    },
                                            }
                                    },
                                },
            }
        );

        return suggestedPresets;
    }
}
