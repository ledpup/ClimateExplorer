namespace ClimateExplorer.Web.UiLogic;

using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

public static class ChartLogic
{
    private const int MaxDropoutYears = 5;

    public static string BuildChartTitle(List<SeriesWithData> chartSeriesWithData, Dictionary<Guid, Location>? locationDictionary)
    {
        var specs = chartSeriesWithData
            .SelectMany(x => x.ChartSeries!.SourceSeriesSpecifications!);

        var ids = specs.Select(x => x.LocationId).Distinct().ToArray();
        var names = specs.Select(x => x.LocationName).Distinct().ToArray();

        var singleLocation = ids.Length == 1 && locationDictionary is not null && locationDictionary.TryGetValue(ids.Single(), out var location)
            ? location
            : null;

        if (chartSeriesWithData.Count == 1)
        {
            return chartSeriesWithData.Single().ChartSeries!.GetFriendlyTitle(singleLocation?.DisplayTitle);
        }

        if (singleLocation is not null)
        {
            return singleLocation.DisplayTitle;
        }
        else if (ids.Length > 0)
        {
            return string.Join(", ", names);
        }

        return "Climate data";
    }

    public static string BuildChartSubtitle(BinIdentifier? chartStartBin, BinIdentifier? chartEndBin, BinGranularities binGranularity)
    {
        return (chartStartBin != null && chartEndBin != null)
            ? chartStartBin is YearBinIdentifier
                ? $"{chartStartBin.Label}-{chartEndBin.Label}, {Convert.ToInt16(chartEndBin.Label) - Convert.ToInt16(chartStartBin.Label)} years"
                : $"{chartStartBin.Label}-{chartEndBin.Label}"
            : binGranularity.ToFriendlyString();
    }

    /// <summary>
    /// The aggregation settings, shown in the chart's top-right corner rather than in the subtitle.
    /// </summary>
    public static string BuildAggregationText(short groupingDays, string groupingThresholdText)
    {
        return $"Aggregation: {AggregationSettings.GroupingDaysText(groupingDays)} at {groupingThresholdText}";
    }

    public static ChartJsDataset GetLineChartDataset(
        string label,
        List<double?> values,
        string chartColour,
        UnitOfMeasure unitOfMeasure,
        SeriesTransformations seriesTransformations,
        string? customTransformation,
        bool renderSmallPoints,
        SeriesAggregationOptions seriesAggregationOptions,
        string? yAxisId = null)
    {
        var colour = Enumerable.Repeat<string?>(chartColour, values.Count).ToList();

        var lineChartDataset =
            new ChartJsDataset
            {
                Type = "line",
                Label = label,
                Data = values,
                BackgroundColor = colour,
                BorderColor = colour,
                Fill = false,
                PointRadius = renderSmallPoints ? 0.1f : 3f,
                ShowLine = true,
                PointBackgroundColor = "transparent",
                PointBorderWidth = 1,
                PointBorderColor = colour,
                PointHoverBackgroundColor = colour,
                BorderDash = [],
                BorderWidth = 4,

                YAxisId = yAxisId ?? GetYAxisId(seriesTransformations, customTransformation, unitOfMeasure, seriesAggregationOptions),
            };

        return lineChartDataset;
    }

    public static ChartJsDataset GetScatterChartDataset(
        string label,
        List<double?> values,
        string chartColour,
        UnitOfMeasure unitOfMeasure,
        SeriesTransformations seriesTransformations,
        string? customTransformation,
        SeriesAggregationOptions seriesAggregationOptions,
        string? yAxisId = null)
    {
        var colour = Enumerable.Repeat<string?>(chartColour, values.Count).ToList();

        return
            new ChartJsDataset
            {
                Type = "line",
                Label = label,
                Data = values,
                BackgroundColor = colour,
                BorderColor = colour,
                Fill = false,
                PointRadius = 3f,
                PointHoverRadius = 5f,
                ShowLine = false,
                PointBorderColor = colour,
                PointHoverBackgroundColor = colour,
                BorderWidth = 0,

                YAxisId = yAxisId ?? GetYAxisId(seriesTransformations, customTransformation, unitOfMeasure, seriesAggregationOptions),
            };
    }

    /// <param name="colourValues">
    /// The values to choose each bar's colour from by sign, when they differ from the plotted
    /// <paramref name="values"/>: the metric values of a converted series, so a bar is coloured the same
    /// whichever unit system it's shown in. Defaults to <paramref name="values"/>.
    /// </param>
    public static ChartJsDataset GetBarChartDataset(
        string label,
        List<double?> values,
        string chartColour,
        UnitOfMeasure unitOfMeasure,
        bool? absoluteValues,
        Colours? positiveValueColour,
        Colours? negativeValueColour,
        SeriesTransformations seriesTransformations,
        string? customTransformation,
        SeriesAggregationOptions seriesAggregationOptions,
        string? yAxisId = null,
        List<double?>? colourValues = null)
    {
        var colour = GetBarChartColourSet(colourValues ?? values, chartColour, positiveValueColour, negativeValueColour);

        return
            new ChartJsDataset
            {
                Type = "bar",
                Label = label,
                Data = values.Select(x => absoluteValues.GetValueOrDefault() && x.HasValue ? Math.Abs(x.Value) : x).ToList(),
                BorderColor = colour,
                BackgroundColor = colour,
                YAxisId = yAxisId ?? GetYAxisId(seriesTransformations, customTransformation, unitOfMeasure, seriesAggregationOptions),
            };
    }

    public static ChartJsDataset GetChartDataset(
        string label,
        List<double?> values,
        UnitOfMeasure unitOfMeasure,
        SeriesDisplayStyle displayStyle,
        string chartColour,
        bool? absoluteValues = false,
        Colours? positiveValueColour = null,
        Colours? negativeValueColour = null,
        SeriesTransformations seriesTransformations = SeriesTransformations.Identity,
        string? customTransformation = null,
        SeriesAggregationOptions seriesAggregationOptions = SeriesAggregationOptions.Mean,
        bool renderSmallPoints = false,
        string? yAxisId = null,
        List<double?>? colourValues = null)
    {
        return displayStyle switch
        {
            SeriesDisplayStyle.Line => GetLineChartDataset(label, values, chartColour, unitOfMeasure, seriesTransformations, customTransformation, renderSmallPoints, seriesAggregationOptions, yAxisId),
            SeriesDisplayStyle.Bar => GetBarChartDataset(label, values, chartColour, unitOfMeasure, absoluteValues, positiveValueColour, negativeValueColour, seriesTransformations, customTransformation, seriesAggregationOptions, yAxisId, colourValues),
            SeriesDisplayStyle.Scatter => GetScatterChartDataset(label, values, chartColour, unitOfMeasure, seriesTransformations, customTransformation, seriesAggregationOptions, yAxisId),
            _ => throw new NotImplementedException($"SeriesDisplayStyle {displayStyle}"),
        };
    }

    /// <summary>
    /// The dataset for a trend's forward projection: unconnected points in the same colour as the
    /// series they came from, styled like <see cref="GetScatterChartDataset"/>. No line is drawn at
    /// all - a year-by-year scatter reads as "projected, not measured" on its own, without needing a
    /// dash pattern or a colour offset from the real data. Pinned to the parent's y-axis so the two
    /// are read against the same scale.
    /// </summary>
    public static ChartJsDataset GetTrendChartDataset(
        string label,
        List<double?> values,
        string chartColour,
        string yAxisId)
    {
        var colour = Enumerable.Repeat<string?>(chartColour, values.Count).ToList();

        return
            new ChartJsDataset
            {
                Type = "line",
                Label = label,
                Data = values,
                BackgroundColor = colour,
                BorderColor = colour,
                Fill = false,
                PointRadius = 3f,
                PointHoverRadius = 5f,
                ShowLine = false,
                PointBorderColor = colour,
                PointHoverBackgroundColor = colour,
                BorderWidth = 0,

                YAxisId = yAxisId,
            };
    }

    public static ChartJsTrendline CreateTrendline(string colour)
    {
        return new ChartJsTrendline(colour, "solid", 3);
    }

    public static string GetXAxisLabel(BinGranularities binGranularity)
    {
        return binGranularity switch
        {
            BinGranularities.ByYear => "Year",
            BinGranularities.ByYearAndMonth => "Month and year",
            BinGranularities.ByYearAndWeek => "Week and year",
            BinGranularities.ByYearAndDay => "Date",
            BinGranularities.ByMonthOnly => "Month of the year",
            BinGranularities.ByDayOnly => "Day of the year",
            BinGranularities.BySouthernHemisphereTemperateSeasonOnly => "Southern hemisphere temperate season",
            BinGranularities.BySouthernHemisphereTropicalSeasonOnly => "Southern hemisphere tropical season",
            _ => throw new NotImplementedException($"BinGranularity {binGranularity}"),
        };
    }

    /// <param name="displayUnits">
    /// The units to plot the series in. Defaults to metric. The data set's values are metric and are
    /// converted here, as the last step before Chart.js; bar colours are still chosen from the metric
    /// values.
    /// </param>
    public static ChartJsDataset GetSeriesChartDataset(
        SeriesWithData chartSeries,
        DataSet dataSet,
        string label,
        string htmlColourCode,
        bool absoluteValues = false,
        bool renderSmallPoints = false,
        SeriesDisplayUnits? displayUnits = null)
    {
        var unitOfMeasure = dataSet.MeasurementDefinition!.UnitOfMeasure;

        displayUnits ??= SeriesDisplayUnits.Resolve(chartSeries.ChartSeries!, unitOfMeasure, UnitFormatter.Metric);

        var metricValues =
            dataSet.DataRecords
            .Select(x => x.Value)
            .ToList();

        var values = metricValues.Select(displayUnits.ToDisplay).ToList();

        chartSeries.ChartSeries!.Colour = htmlColourCode;

        var chartDataset = GetChartDataset(label, values, unitOfMeasure, chartSeries.ChartSeries.DisplayStyle, htmlColourCode, absoluteValues, chartSeries.ChartSeries.PositiveValueColour, chartSeries.ChartSeries.NegativeValueColour, chartSeries.ChartSeries.SeriesTransformation, chartSeries.ChartSeries.CustomTransformation, chartSeries.ChartSeries.Aggregation, renderSmallPoints, displayUnits.AxisId, metricValues);

        return chartSeries.ChartSeries.ShowTrendline
            ? chartDataset with { TrendlineLinear = CreateTrendline(htmlColourCode) }
            : chartDataset;
    }

    public static Tuple<BinIdentifier, BinIdentifier> GetBinRangeToPlotForGaplessRange(
        IEnumerable<DataSet> preProcessedDataSets,
        bool chartAllData,
        string selectedStartYear,
        string selectedEndYear)
    {
        // Parse the start and end years, if any, specified by the user
        var userStartYear = string.IsNullOrEmpty(selectedStartYear) ? null : (short?)short.Parse(selectedStartYear);
        var userEndYear = string.IsNullOrEmpty(selectedEndYear) ? null : (short?)short.Parse(selectedEndYear);

        // Analyse the data we want to plot, to find the first & last bin we have a value for, for each data set
        var firstBinInEachDataSet =
            preProcessedDataSets
            .Select(x => x.GetFirstDataRecordWithValueInDataSet())
            .Select(x => (BinIdentifierForGaplessBin)x.BinIdentifier!);

        var lastBinInEachDataSet =
            preProcessedDataSets
            .Select(x => x.GetLastDataRecordWithValueInDataSet())
            .Select(x => (BinIdentifierForGaplessBin)x.BinIdentifier!);

        var firstBinAcrossAllDataSets = firstBinInEachDataSet.Min();
        var lastFirstBinAcrossAllDataSets = firstBinInEachDataSet.Max();
        var lastBinAcrossAllDataSets = lastBinInEachDataSet.Max();

        BinIdentifierForGaplessBin? startBin = null;

        if (chartAllData)
        {
            startBin = firstBinAcrossAllDataSets!;
        }
        else if (userStartYear.HasValue)
        {
            if (userStartYear.Value > firstBinAcrossAllDataSets!.FirstDayInBin.Year)
            {
                if (firstBinAcrossAllDataSets is YearBinIdentifier)
                {
                    startBin = new YearBinIdentifier(userStartYear.Value);
                }

                if (firstBinAcrossAllDataSets is YearAndMonthBinIdentifier)
                {
                    startBin = new YearAndMonthBinIdentifier(userStartYear.Value, 1);
                }

                if (firstBinAcrossAllDataSets is YearAndWeekBinIdentifier)
                {
                    startBin = new YearAndWeekBinIdentifier(userStartYear.Value, 1);
                }

                if (firstBinAcrossAllDataSets is YearAndDayBinIdentifier)
                {
                    startBin = new YearAndDayBinIdentifier(userStartYear.Value, 1, 1);
                }
            }
            else
            {
                // The requested start year is at or before the earliest data we actually have
                // (e.g. a preset's StartYear matches one series but predates another that got
                // removed). Fall back to the earliest bin actually present rather than leaving
                // startBin unset, which would otherwise crash chart rendering.
                startBin = firstBinAcrossAllDataSets;
            }
        }
        else
        {
            // Start where the last series starts, so the chart only covers the period the series
            // have in common. That only works if every series is actually reporting at that point -
            // a series can start, stop, and restart after another one has started, in which case
            // cropping would hide its early data without giving a common period in return. When
            // that happens, chart from the beginning instead.
            var allDataSetsReportingAtLastStart =
                preProcessedDataSets.All(x => IsReportingAt(x, lastFirstBinAcrossAllDataSets!));

            startBin = allDataSetsReportingAtLastStart ? lastFirstBinAcrossAllDataSets! : firstBinAcrossAllDataSets!;
        }

        var endBin = lastBinAcrossAllDataSets;

        if (userEndYear != null)
        {
            if (userEndYear.Value < endBin!.FirstDayInBin.Year)
            {
                if (endBin is YearBinIdentifier)
                {
                    endBin = new YearBinIdentifier(userEndYear.Value);
                }

                if (endBin is YearAndMonthBinIdentifier)
                {
                    endBin = new YearAndMonthBinIdentifier(userEndYear.Value, 1);
                }

                if (endBin is YearAndWeekBinIdentifier)
                {
                    endBin = new YearAndWeekBinIdentifier(userEndYear.Value, 1);
                }

                if (endBin is YearAndDayBinIdentifier)
                {
                    endBin = new YearAndDayBinIdentifier(userEndYear.Value, 1, 1);
                }
            }
        }

        return new Tuple<BinIdentifier, BinIdentifier>(startBin!, endBin!);
    }

    public static string GetYAxisId(SeriesTransformations seriesTransformations, string? customTransformation, UnitOfMeasure unitOfMeasure, SeriesAggregationOptions seriesAggregationOptions)
    {
        return seriesTransformations switch
        {
            SeriesTransformations.DayOfYearIfFrost => seriesAggregationOptions == SeriesAggregationOptions.Maximum ? "lastDayOfFrost" : "firstDayOfFrost",
            SeriesTransformations.Custom => ChartSeriesDefinition.GetFriendlyCustomTransformationLabel(customTransformation ?? "Custom").Replace(" ", string.Empty),
            _ => unitOfMeasure.ToString().ToLowerFirstChar(),
        };
    }

    public static List<string?> GetBarChartColourSet(List<double?> values, string chartColour, Colours? positiveValueColour = null, Colours? negativeValueColour = null)
    {
        var count = values.Count;

        // Default positive/negative colours, used whenever a series doesn't request its own via
        // PositiveValueColour/NegativeValueColour - matches this chart's long-standing default.
        var positiveColour = positiveValueColour.HasValue ? ColourServer.GetHtmlColourCode(positiveValueColour.Value) : "rgba(255,63,63,1)";
        var negativeColour = negativeValueColour.HasValue ? ColourServer.GetHtmlColourCode(negativeValueColour.Value) : "rgba(63,63,255,1)";

        var hasPositive = values.Any(v => v.HasValue && v.Value > 0);
        var hasNegative = values.Any(v => v.HasValue && v.Value < 0);
        var useMixedColours = hasPositive && hasNegative;

        var colour = new List<string?>();
        for (var i = 0; i < count; i++)
        {
            if (values[i].HasValue)
            {
                string barColour;
                if (useMixedColours)
                {
                    barColour = values[i]!.Value > 0 ? positiveColour : negativeColour;
                }
                else
                {
                    barColour = chartColour;
                }

                colour.Add(barColour);
            }
            else
            {
                colour.Add(null);
            }
        }

        return colour;
    }

    /// <summary>
    /// Whether a data set is reporting at the given bin: it either has a value there, or the bin
    /// falls in a short dropout - the values either side of it are no more than
    /// <see cref="MaxDropoutYears"/> apart - rather than in a stretch where the series had stopped.
    /// </summary>
    private static bool IsReportingAt(DataSet dataSet, BinIdentifierForGaplessBin bin)
    {
        BinIdentifierForGaplessBin? previous = null;

        foreach (var record in dataSet.DataRecords.Where(x => x.Value.HasValue))
        {
            var current = (BinIdentifierForGaplessBin)record.BinIdentifier!;

            if (current.FirstDayInBin >= bin.FirstDayInBin)
            {
                return current.FirstDayInBin == bin.FirstDayInBin
                    || (previous != null && current.FirstDayInBin <= previous.FirstDayInBin.AddYears(MaxDropoutYears));
            }

            previous = current;
        }

        return false;
    }
}
