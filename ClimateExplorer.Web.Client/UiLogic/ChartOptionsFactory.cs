namespace ClimateExplorer.Web.UiLogic;

using System.Dynamic;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Builds the Chart.js options object (including scales and per-unit y axes) for a prepared set of
/// chart series. This is the Phase 6 extraction of the option-building helpers that previously lived
/// in <c>ChartView</c> (<c>CreateChartOptions</c>, <c>BuildChartScales</c>, <c>CreateYAxes</c>,
/// <c>CreateAxesMinMax</c>), keeping <c>ChartView</c> focused on interaction and Chart.js interop.
/// </summary>
public static class ChartOptionsFactory
{
    public static ChartOptionsBuildResult Build(ChartOptionsRequest request)
    {
        var (options, axes) = BuildOptionsAndAxes(request);
        return new ChartOptionsBuildResult(options, axes);
    }

    /// <summary>
    /// Builds a global min/max per axis from the full source datasets, before any display range filtering.
    /// This ensures the y-axis range reflects the complete dataset even when "chart all data" is off.
    /// </summary>
    public static (Dictionary<string, (double Min, double Max)> AxisMinMax, HashSet<string> AxisHasBarSeries) CalculateAxisMinMax(
        IReadOnlyList<SeriesWithData> seriesWithData)
    {
        Dictionary<string, (double Min, double Max)> axisMinMax = [];
        HashSet<string> axisHasBarSeries = [];
        foreach (var swd in seriesWithData)
        {
            var cs = swd.ChartSeries!;
            var uom = cs.SourceSeriesSpecifications!.First().MeasurementDefinition!.UnitOfMeasure;
            var axisId = ChartLogic.GetYAxisId(cs.SeriesTransformation, cs.CustomTransformation, uom, cs.Aggregation);

            if (cs.DisplayStyle == SeriesDisplayStyle.Bar)
            {
                axisHasBarSeries.Add(axisId);
            }

            var values = swd.PreProcessedDataSet!.DataRecords!
                .Select(x => x.Value)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (values.Count == 0)
            {
                continue;
            }

            var seriesMin = values.Min();
            var seriesMax = values.Max();

            if (axisMinMax.TryGetValue(axisId, out var current))
            {
                axisMinMax[axisId] = (Math.Min(current.Min, seriesMin), Math.Max(current.Max, seriesMax));
            }
            else
            {
                axisMinMax[axisId] = (seriesMin, seriesMax);
            }
        }

        return (axisMinMax, axisHasBarSeries);
    }

    private static (object Options, IReadOnlyList<AxisInfo> Axes) BuildOptionsAndAxes(ChartOptionsRequest request)
    {
        dynamic scales = new ExpandoObject();

        var xLabel = ChartLogic.GetXAxisLabel(request.BinGranularity);

        // The casing on the names of the scales is case-sensitive.
        // The x axis needs to be x (not X) or the chart will not register the scale correctly.
        // This will at least break the trendlines component
        scales.x = new
        {
            Grid = new { DrawOnChartArea = false },
            Title = new
            {
                Text = xLabel,
                Display = true,
                Color = "black",
            },
        };

        var axes = CreateYAxes(scales, request);

        var options = CreateChartOptions(request, scales);

        return (options, axes);
    }

    private static IReadOnlyList<AxisInfo> CreateYAxes(dynamic scales, ChartOptionsRequest request)
    {
        var (axisMinMax, axisHasBarSeries) = CalculateAxisMinMax(request.SeriesWithData);

        var axes = new List<string>();
        var currentAxes = new List<AxisInfo>();
        foreach (var s in request.Series.Where(x => x.DataAvailable))
        {
            var uom = s.SourceSeriesSpecifications!.First().MeasurementDefinition!.UnitOfMeasure;
            var axisId = ChartLogic.GetYAxisId(s.SeriesTransformation, s.CustomTransformation, uom, s.Aggregation);
            if (!axes.Contains(axisId))
            {
                axisMinMax.TryGetValue(axisId, out var globalMinMax);
                var axisRange = globalMinMax.Max - globalMinMax.Min;
                var axisPadding = !axisHasBarSeries.Contains(axisId) ? axisRange * 0.02 : 0.0;
                var scaleToZero = request.AxesScaleToZero.TryGetValue(axisId, out var s2z) && s2z;
                var label = UnitOfMeasureLabel(s.SeriesTransformation, s.CustomTransformation, uom, s.Aggregation, s.Value);
                currentAxes.Add(new AxisInfo(axisId, label));
                ((IDictionary<string, object>)scales).Add(
                    axisId,
                    new
                    {
                        Display = true,
                        Axis = "y",
                        Position = axes.Count % 2 == 0 ? "left" : "right",
                        Grid = new { DrawOnChartArea = axes.Count == 0 },
                        Title = new
                        {
                            Text = label,
                            Display = true,
                            Color = s.Colour,
                        },
                        Min = scaleToZero && globalMinMax.Min > 0 ? 0.0 : globalMinMax.Min == 0 ? globalMinMax.Min : globalMinMax.Min - axisPadding,
                        Max = globalMinMax.Max == 0 ? globalMinMax.Max : globalMinMax.Max + axisPadding,
                    });
                axes.Add(axisId);
            }
        }

        return currentAxes;
    }

    private static object CreateChartOptions(ChartOptionsRequest request, dynamic scales)
    {
        return new
        {
            Animation = false,
            Responsive = true,
            MaintainAspectRatio = false,
            SpanGaps = false,
            Elements = new
            {
                Point = new
                {
                    HitRadius = request.IsMobileDevice ? 5 : 10,
                    HoverRadius = request.IsMobileDevice ? 3 : 6,
                },
            },
            Plugins = new
            {
                Title = new
                {
                    Text = request.Title,
                    Display = true,
                    Color = "black",
                },
                Subtitle = new
                {
                    Text = request.Subtitle,
                    Display = true,
                    Color = "black",
                },
                Tooltip = new
                {
                    Mode = request.IsMobileDevice ? "nearest" : "index",
                    Intersect = false,
                    BoxPadding = 4,
                },
                Legend = new
                {
                    Position = "bottom",
                },
            },
            Scales = scales,
        };
    }
}
