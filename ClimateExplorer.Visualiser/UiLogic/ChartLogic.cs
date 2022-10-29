using ClimateExplorer.Core;
using ClimateExplorer.Visualiser.Pages;
using ClimateExplorer.Visualiser.UiModel;
using Blazorise.Charts;
using Blazorise.Charts.Trendline;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Visualiser.UiLogic;

public static class ChartLogic
{
    public static string BuildChartTitle(List<SeriesWithData> chartSeriesWithData)
    {
        if (chartSeriesWithData.Count == 1)
        {
            return chartSeriesWithData.Single().ChartSeries.FriendlyTitle;
        }

        var locationNames =
            chartSeriesWithData
            .SelectMany(x => x.ChartSeries.SourceSeriesSpecifications)
            .Select(x => x.LocationName)
            .Where(x => x != null)
            .Distinct()
            .ToArray();

        if (locationNames.Length > 0)
        {
            return String.Join(", ", locationNames);
        }

        return "Climate data";
    }

    public static LineChartDataset<float?> GetLineChartDataset(
        string label, 
        List<float?> values, 
        ChartColor chartColor, 
        UnitOfMeasure unitOfMeasure,
        SeriesTransformations seriesTransformations,
        bool renderSmallPoints)
    {
        var count = values.Count;
        var colour = new List<string>();
        for (var i = 0; i < count; i++)
            colour.Add(chartColor);

        var lineChartDataset =
            new LineChartDataset<float?>
            {
                Label = label,
                Data = values,
                BackgroundColor = colour,
                BorderColor = colour,
                Fill = false,
                PointRadius = renderSmallPoints ? 0.1f : 3,
                ShowLine = true,
                PointBorderColor = renderSmallPoints ? colour : "#bbb",
                PointHoverBackgroundColor = colour,
                BorderDash = new List<int> { },
                //Tension = 0.1f,
                YAxisID = GetYAxisId(seriesTransformations, unitOfMeasure),
            };

        return lineChartDataset;
    }

    public static BarChartDataset<float?> GetBarChartDataset(
        string label, 
        List<float?> values, 
        UnitOfMeasure unitOfMeasure, 
        bool? absoluteValues, 
        bool redPositive,
        SeriesTransformations seriesTransformations)
    {
        var colour = Enso.GetBarChartColourSet(values, seriesTransformations == SeriesTransformations.IsFrosty ? false : redPositive);

        return 
            new BarChartDataset<float?>
            {
                Label = label,
                Data = values.Select(x => absoluteValues.GetValueOrDefault() && x.HasValue ? MathF.Abs(x.Value) : x).ToList(),
                BorderColor = colour,
                BackgroundColor = colour,
                YAxisID = GetYAxisId(seriesTransformations, unitOfMeasure),
            };
    }

    public static ChartDataset<float?> GetChartDataset(
        string label, 
        List<float?> values, 
        UnitOfMeasure unitOfMeasure, 
        ChartType chartType, 
        ChartColor? chartColour = null, 
        bool? absoluteValues = false, 
        bool redPositive = true,
        SeriesTransformations seriesTransformations = SeriesTransformations.Identity,
        SeriesAggregationOptions seriesAggregationOptions = SeriesAggregationOptions.Mean,
        bool renderSmallPoints = false)
    {
        switch (chartType)
        {
            case ChartType.Line:
                if (!chartColour.HasValue)
                {
                    throw new NullReferenceException(nameof(chartColour));
                }
                return GetLineChartDataset(label, values, chartColour.Value, unitOfMeasure, seriesTransformations, renderSmallPoints);
            case ChartType.Bar:
                return GetBarChartDataset(label, values, unitOfMeasure, absoluteValues, redPositive, seriesTransformations);
        }

        throw new NotImplementedException($"ChartType {chartType}");
    }

    public static ChartTrendlineData CreateTrendline(int datasetIndex, ChartColor colour)
    {
        return
            new ChartTrendlineData
            {
                DatasetIndex = datasetIndex,
                Width = 3,
                Color = colour
            };
    }

    public static string GetXAxisLabel(BinGranularities binGranularity)
    {
        switch (binGranularity)
        {
            case BinGranularities.ByYear:
                return "Year";
            case BinGranularities.ByYearAndMonth:
                return "Month";
            case BinGranularities.ByMonthOnly:
                return "Month of the year";
            case BinGranularities.BySouthernHemisphereTemperateSeasonOnly:
                return "Southern hemisphere temperate season";
            case BinGranularities.BySouthernHemisphereTropicalSeasonOnly:
                return "Southern hemisphere tropical season";
            default:
                throw new NotImplementedException($"BinGranularity {binGranularity}");
        }
    }

    public static async Task AddDataSetToChart(
        Chart<float?> chart,
        SeriesWithData chartSeries,
        DataSet dataSet,
        string label,
        string htmlColourCode,
        bool absoluteValues = false,
        bool redPositive = true,
        bool renderSmallPoints = false)
    {
        var values =
            dataSet.DataRecords
            .Select(x => x.Value)
            .ToList();

        var colour = ChartColor.FromHtmlColorCode(htmlColourCode);

        chartSeries.ChartSeries.Colour = htmlColourCode;

        var chartType =
            chartSeries.ChartSeries.DisplayStyle == SeriesDisplayStyle.Line
            ? ChartType.Line
            : ChartType.Bar;

        var chartDataset = GetChartDataset(label, values, dataSet.MeasurementDefinition.UnitOfMeasure, chartType, colour, absoluteValues, redPositive, chartSeries.ChartSeries.SeriesTransformation, chartSeries.ChartSeries.Aggregation, renderSmallPoints);

        await chart.AddDataSet(chartDataset);
    }

    public static Tuple<BinIdentifier, BinIdentifier> GetBinRangeToPlotForGaplessRange(
        IEnumerable<DataSet> preProcessedDataSets,
        bool useMostRecentStartYear,
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
            .Select(x => (BinIdentifierForGaplessBin)x.GetBinIdentifier());

        var lastBinInEachDataSet =
            preProcessedDataSets
            .Select(x => x.GetLastDataRecordWithValueInDataSet())
            .Select(x => (BinIdentifierForGaplessBin)x.GetBinIdentifier());

        var firstBinAcrossAllDataSets = firstBinInEachDataSet.Min();
        var lastFirstBinAcrossAllDataSets = firstBinInEachDataSet.Max();
        var lastBinAcrossAllDataSets = lastBinInEachDataSet.Max();

        var startBin = useMostRecentStartYear ? lastFirstBinAcrossAllDataSets : firstBinAcrossAllDataSets;
        var endBin = lastBinAcrossAllDataSets;

        if (userStartYear != null)
        {
            if (userStartYear.Value > startBin.FirstDayInBin.Year)
            {
                if (startBin is YearBinIdentifier)
                {
                    startBin = new YearBinIdentifier(userStartYear.Value);
                }

                if (startBin is YearAndMonthBinIdentifier)
                {
                    startBin = new YearAndMonthBinIdentifier(userStartYear.Value, 1);
                }
            }
        }

        if (userEndYear != null)
        {
            if (userEndYear.Value < endBin.FirstDayInBin.Year)
            {
                if (endBin is YearBinIdentifier)
                {
                    endBin = new YearBinIdentifier(userEndYear.Value);
                }

                if (endBin is YearAndMonthBinIdentifier)
                {
                    endBin = new YearAndMonthBinIdentifier(userEndYear.Value, 1);
                }
            }
        }

        return new Tuple<BinIdentifier, BinIdentifier>(startBin, endBin);
    }

    public static string GetYAxisId(SeriesTransformations seriesTransformations, UnitOfMeasure unitOfMeasure)
    {
        return seriesTransformations switch
        {
            SeriesTransformations.IsFrosty => "daysOfFrost",
            SeriesTransformations.DayOfYearIfFrost => "dayOfYear",
            SeriesTransformations.EqualOrAbove35 => "daysEqualOrAbove35",
            SeriesTransformations.EqualOrAbove1 => "daysEqualOrAbove1",
            SeriesTransformations.EqualOrAbove1AndLessThan10 => "daysEqualOrAbove1LessThan10",
            SeriesTransformations.EqualOrAbove10 => "daysEqualOrAbove10",
            SeriesTransformations.EqualOrAbove10AndLessThan25 => "daysEqualOrAbove10LessThan25",
            SeriesTransformations.EqualOrAbove25 => "daysEqualOrAbove25",
            _ => unitOfMeasure.ToString().ToLowerFirstChar()
        };
    }
}
