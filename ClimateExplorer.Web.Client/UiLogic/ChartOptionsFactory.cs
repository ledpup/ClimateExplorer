namespace ClimateExplorer.Web.UiLogic;

using System.Dynamic;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.Units;
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
    /// The range is in display units, so axis padding and scale-to-zero apply to what the reader sees.
    /// </summary>
    public static (Dictionary<string, (double Min, double Max)> AxisMinMax, HashSet<string> AxisHasBarSeries) CalculateAxisMinMax(
        IReadOnlyList<SeriesWithData> seriesWithData,
        IUnitFormatter? units = null)
    {
        Dictionary<string, (double Min, double Max)> axisMinMax = [];
        HashSet<string> axisHasBarSeries = [];
        foreach (var swd in seriesWithData)
        {
            var cs = swd.ChartSeries!;
            var uom = cs.SourceSeriesSpecifications!.First().MeasurementDefinition!.UnitOfMeasure;
            var displayUnits = SeriesDisplayUnits.Resolve(cs, uom, units ?? UnitFormatter.Metric);
            var axisId = displayUnits.AxisId;

            if (cs.DisplayStyle == SeriesDisplayStyle.Bar)
            {
                axisHasBarSeries.Add(axisId);
            }

            var values = swd.PreProcessedDataSet!.DataRecords!
                .Select(x => x.Value)
                .Where(v => v.HasValue)
                .Select(v => displayUnits.ToDisplay(v!.Value))
                .ToList();

            // A projection can rise above (or fall below) everything in the record, and these
            // min/max values are applied to the axis explicitly - without folding every trend's
            // projection in, a trend line would be drawn outside the axis and clipped.
            foreach (var trend in swd.Trends)
            {
                if (trend.Projection is { } projection)
                {
                    values.AddRange(projection.Predictions.Select(x => displayUnits.ToDisplay(x.PredictedY)));
                }
            }

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
        var (axisMinMax, axisHasBarSeries) = CalculateAxisMinMax(request.SeriesWithData, request.Units);

        var seriesWithAxis = request.Series
            .Where(x => x.DataAvailable)
            .Select(s =>
            {
                var uom = s.SourceSeriesSpecifications!.First().MeasurementDefinition!.UnitOfMeasure;
                var displayUnits = SeriesDisplayUnits.Resolve(s, uom, request.Units);
                return (Series: s, Uom: uom, DisplayUnits: displayUnits, AxisId: displayUnits.AxisId);
            })
            .ToList();

        var coloursByAxis = seriesWithAxis
            .GroupBy(x => x.AxisId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Series.Colour).Distinct().ToArray());

        var axes = new List<string>();
        var currentAxes = new List<AxisInfo>();
        foreach (var (s, uom, displayUnits, axisId) in seriesWithAxis)
        {
            if (!axes.Contains(axisId))
            {
                axisMinMax.TryGetValue(axisId, out var globalMinMax);
                var axisRange = globalMinMax.Max - globalMinMax.Min;
                var axisPadding = !axisHasBarSeries.Contains(axisId) ? axisRange * 0.02 : 0.0;
                var scaleToZero = request.AxesScaleToZero.TryGetValue(axisId, out var s2z) && s2z;
                var label = GetYAxisLabel(s, uom, displayUnits);
                currentAxes.Add(new AxisInfo(axisId, label));

                // Hard Min/Max make Chart.js label the exact bounds (e.g. 11.7, 12.0, 12.5), which looks wrong.
                // Where a bound is padded, suggest it instead: with the default bounds: 'ticks', Chart.js then
                // rounds the axis out to the next tick label once it has chosen the label step.

                // ExpandoObject keys are not run through the camelCase naming policy, so they are written camelCased here.
                var axisScale = new Dictionary<string, object>();
                axisScale["display"] = true;
                axisScale["axis"] = "y";
                axisScale["position"] = axes.Count % 2 == 0 ? "left" : "right";
                axisScale["grid"] = new { DrawOnChartArea = axes.Count == 0 };
                axisScale["title"] = new
                {
                    Text = label,
                    Display = true,
                    Color = "black",
                    SwatchColours = coloursByAxis[axisId],
                };

                if (scaleToZero && globalMinMax.Min > 0)
                {
                    axisScale["min"] = 0.0;
                }
                else if (globalMinMax.Min == 0)
                {
                    axisScale["min"] = 0.0;
                }
                else
                {
                    axisScale["suggestedMin"] = globalMinMax.Min - axisPadding;
                }

                if (globalMinMax.Max == 0)
                {
                    axisScale["max"] = 0.0;
                }
                else
                {
                    axisScale["suggestedMax"] = globalMinMax.Max + axisPadding;
                }

                ((IDictionary<string, object>)scales).Add(axisId, axisScale);
                axes.Add(axisId);
            }
        }

        return currentAxes;
    }

    /// <summary>
    /// The axis title. Only an untransformed series can be converted, so when it is, the title is the
    /// display unit's own label with the same anomaly wording Core adds; otherwise it's Core's metric
    /// title, unchanged.
    /// </summary>
    private static string GetYAxisLabel(ChartSeriesDefinition s, UnitOfMeasure uom, SeriesDisplayUnits displayUnits)
    {
        if (s.SeriesTransformation == SeriesTransformations.Identity && displayUnits.Formatter.Converts(uom))
        {
            var label = displayUnits.Formatter.Label(uom);

            return s.Value == SeriesValueOptions.Anomaly ? $"{label} - Anomaly" : label;
        }

        return UnitOfMeasureLabel(s.SeriesTransformation, s.CustomTransformation, uom, s.Aggregation, s.Value, s.Hemisphere);
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

                    // More than the default bottom padding, so the corner text clears the top tick label of a right-hand axis.
                    Padding = new { Top = 0, Bottom = 15 },
                },

                // Drawn by the cornerNote plugin in ChartJsChart.razor.js. Left off on mobile, where there isn't room.
                CornerNote = new
                {
                    Text = request.IsMobileDevice ? null : request.AggregationText,
                    Color = "#595959",
                },
                Tooltip = new
                {
                    // There are custom tooltips in App.razor
                    Enabled = false,
                    Mode = request.IsMobileDevice ? "nearest" : "index",
                    Intersect = false,
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
