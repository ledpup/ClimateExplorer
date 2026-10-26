namespace ClimateExplorer.Web.UiModel;

using System.Data;
using System.Diagnostics.CodeAnalysis;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using static ClimateExplorer.Core.Enums;

public class ChartSeriesDefinition
{
    /// <summary>The most trends a single series may carry at once.</summary>
    public const int MaxTrends = 3;

    /// <summary>
    /// Used only for uniqueness tracking by UI controls.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // Source data fields
    public SourceSeriesSpecification[]? SourceSeriesSpecifications { get; set; }
    public SeriesDerivationTypes SeriesDerivationType { get; set; }
    public BinGranularities BinGranularity { get; set; }
    public short? Year { get; set; }
    public SeriesTransformations SeriesTransformation { get; set; }
    public string? CustomTransformation { get; set; }

    // Data manipulation fields
    public float? GroupingThreshold { get; set; }

    // Data presentation fields
    public TemporalCalculationOptions TemporalCalculation { get; set; }
    public SeriesSmoothingOptions Smoothing { get; set; }
    public int SmoothingWindow { get; set; }
    public SeriesAggregationOptions Aggregation { get; set; }
    public SeriesValueOptions Value { get; set; }
    public string? Colour { get; set; } // Always allocated by ColourServer
    public Colours RequestedColour { get; set; }

    /// <summary>
    /// Optional per-value-sign colour override: when both this and <see cref="NegativeValueColour"/>
    /// are set, values are coloured by sign (this colour for values &gt; 0, the other for values &lt; 0)
    /// instead of using <see cref="RequestedColour"/>/<see cref="Colour"/> throughout. Currently only
    /// honoured for bar charts (see ChartLogic.GetBarChartColourSet), but the field isn't itself
    /// display-style-specific - a preset sets it, and it's up to the chart-building code for each
    /// display style whether it makes use of it. Not yet exposed in the UI; set only from preset code.
    /// </summary>
    public Colours? PositiveValueColour { get; set; }

    /// <summary>See <see cref="PositiveValueColour"/>.</summary>
    public Colours? NegativeValueColour { get; set; }

    // Rendering option fields
    public SeriesDisplayStyle DisplayStyle { get; set; }
    public bool ShowTrendline { get; set; }

    // Trend module field. The module is "on" whenever this is non-empty. Up to three entries
    // (enforced by the UI and defensively by the URL parser); each is an independent trend
    // request - its own regression type, period and prediction horizon. See
    // ChartSeriesTrendRequest for what each entry carries.
    public List<ChartSeriesTrendRequest> Trends { get; set; } = [];

    // Editing mode fields

    /// <summary>
    /// If IsLocked is set, then the series will remain in place even if the user navigates to another notification.
    /// </summary>
    public bool IsLocked { get; set; }

    // Transient view state
    public bool IsExpanded { get; set; }
    public bool DataAvailable { get; internal set; } = true;
    public DataResolution? MinimumDataResolution { get; set; }

    /// <summary>
    /// The first source location's meteorological hemisphere, derived from <see cref="SourceSeriesSpecification.Hemisphere"/>
    /// (itself resolved from the location's latitude wherever a <see cref="Location"/> is looked up,
    /// same as <see cref="SourceSeriesSpecification.LocationName"/> - never stored redundantly here or
    /// in the URL). Null for series with no single real location (e.g. a region-based "global" series).
    /// Currently only consulted by <see cref="GetTransformationOverrideLabel"/> to orient
    /// DayOfYearIfFrost's "first"/"last day of frost" labels - the Northern Hemisphere's frost season
    /// straddles the calendar year boundary, so within one year-bin the smallest day-of-year value is
    /// actually that winter's last (spring) frost, and the largest is the next winter's first (autumn)
    /// frost - the reverse of the Southern Hemisphere, where frost season sits mid-year and doesn't
    /// straddle the boundary.
    /// </summary>
    public MeteorologicalHemisphere? Hemisphere => SourceSeriesSpecifications?.FirstOrDefault()?.Hemisphere;

    /// <summary>
    /// True when every source series behind this definition comes from a region (e.g. atmosphere,
    /// ocean, a hemisphere) rather than a specific location - a "global" data set such as CO2 or
    /// sea ice extent, as opposed to a location's own observations. Used to route "add a trend to
    /// an existing series" between the local and global data set browsers.
    /// </summary>
    public bool IsGlobalSeries =>
        SourceSeriesSpecifications is { Length: > 0 } specs
        && specs.All(x => Region.IsRegionId(x.LocationId));

    public string FriendlyTitle => SeriesLabelSegment.Join(GetFriendlyTitleSegments());

    public static string BuildFriendlyTitleShortForSeries(SourceSeriesSpecification sss, BinGranularities binGranularity, SeriesAggregationOptions aggregation, short? year = null, bool includeLocation = true)
    {
        return SeriesLabelSegment.Join(BuildFriendlyTitleShortSegmentsForSeries(sss, binGranularity, aggregation, year, includeLocation));
    }

    public static string MapDataTypeToFriendlyName(DataType dataType) => dataType.ToFriendlyName();

    public static string GetFriendlyCustomTransformationLabel(string transformation)
    {
        return transformation switch
        {
            "x == 0" => "Value is zero",
            "x <= 2.2" => "Value is 2.2 or less (frost)",
            "x > 1" => "Value is greater than 1",
            "x >= 1" => "Value is 1 or more",
            "x >= 10" => "Value is 10 or more",
            "x >= 25" => "Value is 25 or more",
            "x >= 35" => "Value is 35 or more",
            _ => transformation,
        };
    }

    /// <summary>
    /// The parts of the friendly title, used by the chart legend. A series with more than one source is
    /// one <see cref="SeriesLabelPart.Whole"/> segment.
    /// </summary>
    /// <param name="includeLocation">False to leave the location out, when the chart title already names it.</param>
    public List<SeriesLabelSegment> GetFriendlyTitleSegments(bool includeLocation = true)
    {
        var segments = new List<SeriesLabelSegment>();

        if (SourceSeriesSpecifications!.Length == 1)
        {
            var sss = SourceSeriesSpecifications.Single();

            if (includeLocation && sss.LocationName != null)
            {
                segments.Add(new(SeriesLabelPart.Location, sss.LocationName));
            }

            if (Year != null)
            {
                segments.Add(new(SeriesLabelPart.Year, Year.ToString()!));
            }

            segments.Add(new(SeriesLabelPart.DataType, MapDataTypeToFriendlyName(sss.MeasurementDefinition!.DataType)));

            if (sss.MeasurementDefinition.DataAdjustment != null)
            {
                segments.Add(new(SeriesLabelPart.Adjustment, sss.MeasurementDefinition.DataAdjustment.ToString()!));
            }
        }
        else
        {
            return [new(SeriesLabelPart.Whole, GetFriendlyTitleShort(includeLocation))];
        }

        if (SeriesTransformation != SeriesTransformations.Identity)
        {
            segments.Add(new(SeriesLabelPart.Transformation, "Transformation: " + GetFriendlySeriesTransformationLabel(SeriesTransformation)));
        }

        // "Statistic" rather than "Aggregation", which is the chart-level grouping shown in the chart's corner.
        if (Aggregation != SeriesAggregationOptions.Mean || (Year == null && BinGranularity == BinGranularities.ByMonthOnly))
        {
            segments.Add(new(SeriesLabelPart.Aggregation, "Statistic: " + Aggregation));
        }

        if (TemporalCalculation == TemporalCalculationOptions.AnnualChange)
        {
            segments.Add(new(SeriesLabelPart.TemporalCalculation, "annual change"));
        }

        if (TemporalCalculation == TemporalCalculationOptions.Cumulative)
        {
            segments.Add(new(SeriesLabelPart.TemporalCalculation, "cumulative"));
        }

        if (Value != SeriesValueOptions.Value)
        {
            segments.Add(new(SeriesLabelPart.Value, "Value: " + Value));
        }

        // Smoothing only happens when the x-axis is linear
        if (BinGranularity.IsLinear())
        {
            switch (Smoothing)
            {
                case SeriesSmoothingOptions.ShrinkingCentredMovingAverage:
                    segments.Add(new(SeriesLabelPart.Smoothing, $"{SmoothingWindow} {(BinGranularity == BinGranularities.ByYear ? "year" : "month")} moving average"));
                    break;
                case SeriesSmoothingOptions.CentredMovingAverage:
                    segments.Add(new(SeriesLabelPart.Smoothing, $"{SmoothingWindow} {(BinGranularity == BinGranularities.ByYear ? "year" : "month")} centred moving average"));
                    break;
                case SeriesSmoothingOptions.Trendline:
                    segments.Add(new(SeriesLabelPart.Smoothing, "Trendline"));
                    break;
            }
        }

        return segments;
    }

    public override string ToString()
    {
        return $"CSD: {BinGranularity} | {Smoothing} | {Aggregation} | {Value} | {DisplayStyle}";
    }

    /// <summary>
    /// Some transformations turn a series' raw value into something that's only meaningful alongside a
    /// description of the transformation itself - e.g. a DayOfYearIfFrost series' values are day-of-year
    /// numbers standing in for "first/last day of frost", and a Custom series' values are a 0/1 flag for
    /// a threshold like "x &gt;= 25". For these, the transformation description replaces the usual
    /// location/data-type wording entirely, rather than being appended to it. Returns null for
    /// transformations (including Identity) that don't need this treatment.
    /// </summary>
    /// <remarks>
    /// Single source of truth for this override, shared by the chart legend (<see cref="SeriesLabeller"/>)
    /// and the tooltip (<see cref="GetTooltipLabel"/>) so the two can't drift out of sync.
    /// </remarks>
    public string? GetTransformationOverrideLabel()
    {
        return SeriesTransformation switch
        {
            SeriesTransformations.DayOfYearIfFrost => GetDayOfYearIfFrostLabel(Aggregation, Hemisphere),
            SeriesTransformations.Custom => GetFriendlyCustomTransformationLabel(CustomTransformation ?? "Custom transformation"),
            _ => null,
        };
    }

    /// <summary>
    /// A trimmed label for the chart tooltip: "Location | Data type | Unit". Unlike FriendlyTitle/
    /// GetFriendlyTitleShort (used for the chart legend), this deliberately omits adjustment,
    /// aggregation, smoothing, and other how-it-was-computed detail that isn't needed once you're
    /// reading values off the tooltip. Transformations covered by <see cref="GetTransformationOverrideLabel"/>
    /// bypass this trimming and use their override label instead, matching the chart legend.
    /// </summary>
    /// <param name="unitLabel">The series' unit as displayed, from <see cref="UiLogic.SeriesDisplayUnits"/>.</param>
    public string GetTooltipLabel(string unitLabel)
    {
        var overrideLabel = GetTransformationOverrideLabel();

        if (overrideLabel != null)
        {
            return overrideLabel;
        }

        var descriptor =
            SourceSeriesSpecifications!.Length == 1
            ? BuildTooltipDescriptorForSeries(SourceSeriesSpecifications.Single())
            : GetFriendlyTitleShort();

        return string.IsNullOrWhiteSpace(descriptor) ? unitLabel : $"{descriptor} | {unitLabel}";
    }

    /// <param name="includeLocation">False to leave the location out, when the chart title already names it.</param>
    public string GetFriendlyTitleShort(bool includeLocation = true)
    {
        return SeriesLabelSegment.Join(GetFriendlyTitleShortSegments(includeLocation));
    }

    /// <summary>
    /// The parts of the short title. A derived series (a difference or an average of several) is one
    /// <see cref="SeriesLabelPart.Whole"/> segment.
    /// </summary>
    /// <param name="includeLocation">False to leave the location out, when the chart title already names it.</param>
    public List<SeriesLabelSegment> GetFriendlyTitleShortSegments(bool includeLocation = true)
    {
        switch (SeriesDerivationType)
        {
            case SeriesDerivationTypes.ReturnSingleSeries:
            case SeriesDerivationTypes.AverageOfAnomaliesInRegion:
                return BuildFriendlyTitleShortSegmentsForSeries(SourceSeriesSpecifications!.Single(), BinGranularity, Aggregation, Year, includeLocation);

            case SeriesDerivationTypes.DifferenceBetweenTwoSeries:
                return [new(SeriesLabelPart.Whole, $"[{BuildFriendlyTitleShortForSeries(SourceSeriesSpecifications![0], BinGranularity, Aggregation, Year, includeLocation)}] minus [{BuildFriendlyTitleShortForSeries(SourceSeriesSpecifications[1], BinGranularity, Aggregation, Year, includeLocation)}]")];

            case SeriesDerivationTypes.AverageOfMultipleSeries:
                return [new(SeriesLabelPart.Whole, BuildAverageMultipleSeriesTitle(SourceSeriesSpecifications!, includeLocation))];

            default: throw new NotImplementedException($"SeriesDerivationType {SeriesDerivationType}");
        }
    }

    /// <param name="units">The reader's unit system. Defaults to metric.</param>
    public string GetFriendlyDescription(IUnitFormatter? units = null)
    {
        var segments = new List<string>();

        string? uomLabel = null;

        if (SourceSeriesSpecifications!.Length == 1)
        {
            var sss = SourceSeriesSpecifications.Single();

            if (sss.MeasurementDefinition?.DataAdjustment != null)
            {
                segments.Add(sss.MeasurementDefinition.DataAdjustment.ToString()!);
            }

            if (sss.MeasurementDefinition != null)
            {
                uomLabel = SeriesDisplayUnits.Resolve(this, sss.MeasurementDefinition.UnitOfMeasure, units ?? UnitFormatter.Metric).ShortLabel;
            }
        }

        if (SeriesTransformation != SeriesTransformations.Identity)
        {
            segments.Add(GetFriendlySeriesTransformationLabel(SeriesTransformation));
        }

        if (Aggregation != SeriesAggregationOptions.Mean)
        {
            segments.Add(Aggregation.ToString());
        }

        if (TemporalCalculation == TemporalCalculationOptions.AnnualChange)
        {
            segments.Add("annual change");
        }

        if (TemporalCalculation == TemporalCalculationOptions.Cumulative)
        {
            segments.Add("cumulative");
        }

        if (Value != SeriesValueOptions.Value)
        {
            segments.Add("Value: " + Value);
        }

        // Smoothing only happens when the x-axis is linear
        if (BinGranularity.IsLinear())
        {
            switch (Smoothing)
            {
                case SeriesSmoothingOptions.ShrinkingCentredMovingAverage:
                    segments.Add($"{SmoothingWindow} {GetSmoothingWindowUnit(BinGranularity)} moving average");
                    break;
                case SeriesSmoothingOptions.CentredMovingAverage:
                    segments.Add($"{SmoothingWindow} {GetSmoothingWindowUnit(BinGranularity)} centred moving average");
                    break;
                case SeriesSmoothingOptions.Trendline:
                    segments.Add("Trendline");
                    break;
            }
        }

        if (uomLabel != null)
        {
            segments.Add(uomLabel);
        }

        return string.Join(" | ", segments);
    }

    private static string GetSmoothingWindowUnit(BinGranularities binGranularity)
    {
        return binGranularity switch
        {
            BinGranularities.ByYear => "year",
            BinGranularities.ByYearAndDay => "day",
            BinGranularities.ByYearAndWeek => "week",
            BinGranularities.ByYearAndMonth => "month",
            _ => throw new NotImplementedException($"BinGranularity {binGranularity}"),
        };
    }

    private static List<SeriesLabelSegment> BuildFriendlyTitleShortSegmentsForSeries(SourceSeriesSpecification sss, BinGranularities binGranularity, SeriesAggregationOptions aggregation, short? year, bool includeLocation)
    {
        var segments = new List<SeriesLabelSegment>();

        if (includeLocation && sss.LocationName != null)
        {
            segments.Add(new(SeriesLabelPart.Location, sss.LocationName));
        }

        if (year != null)
        {
            segments.Add(new(SeriesLabelPart.Year, year.ToString()!));
        }

        if (sss.MeasurementDefinition != null)
        {
            segments.Add(new(SeriesLabelPart.DataType, MapDataTypeToFriendlyName(sss.MeasurementDefinition.DataType)));
        }
        else
        {
            segments.Add(new(SeriesLabelPart.DataType, "[Missing MeasurementDefinition]"));
        }

        if (year == null && binGranularity == BinGranularities.ByMonthOnly)
        {
            segments.Add(new(SeriesLabelPart.Aggregation, aggregation.ToString()));
        }

        return segments;
    }

    private static string BuildTooltipDescriptorForSeries(SourceSeriesSpecification sss)
    {
        var segments = new List<string>();

        if (sss.LocationName != null)
        {
            segments.Add(sss.LocationName);
        }

        if (sss.MeasurementDefinition != null)
        {
            segments.Add(MapDataTypeToFriendlyName(sss.MeasurementDefinition.DataType));
        }

        return string.Join(" | ", segments);
    }

    private static string BuildAverageMultipleSeriesTitle(SourceSeriesSpecification[] sss, bool includeLocation)
    {
        var segments = new List<string>();

        if (sss.All(o => o.LocationName == sss[0].LocationName))
        {
            if (includeLocation)
            {
                segments.Add(sss[0].LocationName!);
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        if (sss.All(o => o.MeasurementDefinition!.DataType == sss[0].MeasurementDefinition!.DataType))
        {
            throw new NotImplementedException();
        }
        else
        {
            segments.Add($"Average {string.Join(", ", sss.Select(x => MapDataTypeToFriendlyName(x.MeasurementDefinition!.DataType)).ToList())}");
        }

        if (sss.All(o => o.MeasurementDefinition!.DataAdjustment == sss[0].MeasurementDefinition!.DataAdjustment))
        {
            segments.Add(sss[0].MeasurementDefinition!.DataAdjustment.ToString()!);
        }
        else
        {
            throw new NotImplementedException();
        }

        return string.Join(" | ", segments);
    }

    private string GetFriendlySeriesTransformationLabel(SeriesTransformations seriesTransformation)
    {
        return seriesTransformation switch
        {
            SeriesTransformations.DayOfYearIfFrost => "Day if frost",
            SeriesTransformations.Custom => GetFriendlyCustomTransformationLabel(CustomTransformation ?? "Custom"),
            _ => seriesTransformation.ToString(),
        };
    }

    public class ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked : IEqualityComparer<ChartSeriesDefinition>
    {
        public static bool BaseComparer(ChartSeriesDefinition? x, ChartSeriesDefinition? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (x.Aggregation != y.Aggregation)
            {
                return false;
            }

            if (x.BinGranularity != y.BinGranularity)
            {
                return false;
            }

            if (x.DisplayStyle != y.DisplayStyle)
            {
                return false;
            }

            if (x.ShowTrendline != y.ShowTrendline)
            {
                return false;
            }

            if (x.Trends.Count != y.Trends.Count)
            {
                return false;
            }

            for (int i = 0; i < x.Trends.Count; i++)
            {
                var trendX = x.Trends[i];
                var trendY = y.Trends[i];

                if (trendX.RegressionType != trendY.RegressionType
                    || trendX.TrendPeriod != trendY.TrendPeriod
                    || trendX.TrendPredictionYears != trendY.TrendPredictionYears
                    || trendX.TrendPredictionTargetYear != trendY.TrendPredictionTargetYear)
                {
                    return false;
                }
            }

            if (x.Smoothing != y.Smoothing)
            {
                return false;
            }

            if (x.SmoothingWindow != y.SmoothingWindow)
            {
                return false;
            }

            if (x.Value != y.Value)
            {
                return false;
            }

            if (x.SeriesTransformation != y.SeriesTransformation)
            {
                return false;
            }

            if (x.CustomTransformation != y.CustomTransformation)
            {
                return false;
            }

            if (x.GroupingThreshold != y.GroupingThreshold)
            {
                return false;
            }

            if (x.SourceSeriesSpecifications!.Length != y.SourceSeriesSpecifications!.Length)
            {
                return false;
            }

            for (int i = 0; i < x.SourceSeriesSpecifications.Length; i++)
            {
                var sssX = x.SourceSeriesSpecifications[i];
                var sssY = y.SourceSeriesSpecifications[i];

                if (sssX.DataSetDefinition != sssY.DataSetDefinition)
                {
                    return false;
                }

                if (sssX.LocationId != sssY.LocationId)
                {
                    return false;
                }

                if (sssX.LocationName != sssY.LocationName)
                {
                    return false;
                }

                if (sssX.MeasurementDefinition != sssY.MeasurementDefinition)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Equals(ChartSeriesDefinition? x, ChartSeriesDefinition? y)
        {
            return BaseComparer(x, y);
        }

        public int GetHashCode([DisallowNull] ChartSeriesDefinition obj)
        {
            int hashCode =
                obj.Aggregation.GetHashCode() ^
                obj.RequestedColour.GetHashCode() ^
                obj.BinGranularity.GetHashCode() ^
                obj.DisplayStyle.GetHashCode() ^
                obj.ShowTrendline.GetHashCode() ^
                obj.Smoothing.GetHashCode() ^
                obj.SmoothingWindow.GetHashCode() ^
                obj.Value.GetHashCode() ^
                (obj.GroupingThreshold == null ? 0 : obj.GroupingThreshold.GetHashCode());

            for (int i = 0; i < obj.SourceSeriesSpecifications!.Length; i++)
            {
                var sss = obj.SourceSeriesSpecifications[i];

                hashCode =
                    hashCode ^
                    sss.DataSetDefinition!.Id.GetHashCode() ^
                    sss.LocationId.GetHashCode() ^
                    sss.MeasurementDefinition!.DataType.GetHashCode() ^
                    sss.MeasurementDefinition.DataAdjustment.GetHashCode();
            }

            for (int i = 0; i < obj.Trends.Count; i++)
            {
                var trend = obj.Trends[i];

                hashCode =
                    hashCode ^
                    trend.RegressionType.GetHashCode() ^
                    trend.TrendPeriod.GetHashCode() ^
                    trend.TrendPredictionYears.GetHashCode() ^
                    trend.TrendPredictionTargetYear.GetHashCode();
            }

            return hashCode;
        }
    }

    public class ChartSeriesDefinitionComparer : IEqualityComparer<ChartSeriesDefinition>
    {
        public bool Equals(ChartSeriesDefinition? x, ChartSeriesDefinition? y)
        {
            var baseComparison = ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked.BaseComparer(x, y);

            if (!baseComparison)
            {
                return false;
            }

            if (x!.IsLocked != y!.IsLocked)
            {
                return false;
            }

            if (x.Year != y.Year)
            {
                return false;
            }

            return true;
        }

        public int GetHashCode([DisallowNull] ChartSeriesDefinition obj)
        {
            int hashCode =
                obj.Aggregation.GetHashCode() ^
                obj.RequestedColour.GetHashCode() ^
                obj.BinGranularity.GetHashCode() ^
                obj.DisplayStyle.GetHashCode() ^
                obj.IsLocked.GetHashCode() ^
                obj.ShowTrendline.GetHashCode() ^
                obj.Smoothing.GetHashCode() ^
                obj.SmoothingWindow.GetHashCode() ^
                obj.Value.GetHashCode() ^
                obj.Year.GetHashCode() ^
                (obj.GroupingThreshold == null ? 0 : obj.GroupingThreshold.GetHashCode());

            for (int i = 0; i < obj.SourceSeriesSpecifications!.Length; i++)
            {
                var sss = obj.SourceSeriesSpecifications[i];

                hashCode =
                    hashCode ^
                    sss.DataSetDefinition!.Id.GetHashCode() ^
                    sss.LocationId.GetHashCode() ^
                    sss.MeasurementDefinition!.DataType.GetHashCode() ^
                    sss.MeasurementDefinition.DataAdjustment.GetHashCode();
            }

            for (int i = 0; i < obj.Trends.Count; i++)
            {
                var trend = obj.Trends[i];

                hashCode =
                    hashCode ^
                    trend.RegressionType.GetHashCode() ^
                    trend.TrendPeriod.GetHashCode() ^
                    trend.TrendPredictionYears.GetHashCode() ^
                    trend.TrendPredictionTargetYear.GetHashCode();
            }

            return hashCode;
        }
    }
}
