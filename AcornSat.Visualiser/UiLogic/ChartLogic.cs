using AcornSat.Core;
using AcornSat.Visualiser.Pages;
using AcornSat.Visualiser.UiModel;
using Blazorise.Charts;
using Blazorise.Charts.Trendline;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using static AcornSat.Core.Enums;

namespace AcornSat.Visualiser.UiLogic
{
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
            SeriesTransformations seriesTransformations)
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
                    BorderColor = colour,
                    Fill = false,
                    PointRadius = 3,
                    ShowLine = true,
                    PointBorderColor = "#eee",
                    PointHoverBackgroundColor = colour,
                    BorderDash = new List<int> { },
                    //Tension = 0.1f,
                    YAxisID = GetYAxisId(seriesTransformations, unitOfMeasure),
                };

            return lineChartDataset;
        }

        private static string GetYAxisId(SeriesTransformations seriesTransformations, UnitOfMeasure unitOfMeasure)
        {
            return seriesTransformations switch
            {
                SeriesTransformations.IsFrosty  => "daysOfFrost",
                SeriesTransformations.Above35   => "daysAbove35C",
                _                               => unitOfMeasure.ToString().ToLowerFirstChar()
            };
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
            SeriesTransformations seriesTransformations = SeriesTransformations.Identity)
        {
            switch (chartType)
            {
                case ChartType.Line:
                    if (!chartColour.HasValue)
                    {
                        throw new NullReferenceException(nameof(chartColour));
                    }
                    return GetLineChartDataset(label, values, chartColour.Value, unitOfMeasure, seriesTransformations);
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
            DataAdjustment? dataAdjustment,
            string label,
            string htmlColourCode,
            bool absoluteValues = false,
            bool redPositive = true)
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

            var chartDataset = GetChartDataset(label, values, dataSet.MeasurementDefinition.UnitOfMeasure, chartType, colour, absoluteValues, redPositive, chartSeries.ChartSeries.SeriesTransformation);

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
    }
}
