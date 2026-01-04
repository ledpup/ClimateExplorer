namespace ClimateExplorer.Web.UiModel;

using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using static ClimateExplorer.Core.Enums;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Core.Model;

public class ChartSeriesDefinition
{
    /// <summary>
    /// Used only for uniqueness tracking by UI controls.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // Source data fields
    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    public SourceSeriesSpecification[]? SourceSeriesSpecifications { get; set; }
    public SeriesDerivationTypes SeriesDerivationType { get; set; }
    public BinGranularities BinGranularity { get; set; }
    public short? Year { get; set; }
    public SeriesTransformations SeriesTransformation { get; set; }

    // Data manipulation fields
    public float? GroupingThreshold { get; set; }

    // Data presentation fields
    public SecondaryCalculationOptions SecondaryCalculation { get; set; }
    public SeriesSmoothingOptions Smoothing { get; set; }
    public int SmoothingWindow { get; set; }
    public SeriesAggregationOptions Aggregation { get; set; }
    public SeriesValueOptions Value { get; set; }
    public string? Colour { get; set; } // Always allocated by ColourServer
    public Colours RequestedColour { get; set; }

    // Rendering option fields
    public SeriesDisplayStyle DisplayStyle { get; set; }
    public bool ShowTrendline { get; set; }

    // Editing mode fields

    /// <summary>
    /// If IsLocked is set, then the series will remain in place even if the user navigates to another notification.
    /// </summary>
    public bool IsLocked { get; set; }

    // Transient view state
    public bool IsExpanded { get; set; }
    public bool DataAvailable { get; internal set; } = true;
    public DataResolution? MinimumDataResolution { get; set; }

    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:Closing parenthesis should be spaced correctly", Justification = "Rule conflict")]
    public static string BuildFriendlyTitleShortForSeries(SourceSeriesSpecification sss, BinGranularities binGranularity, SeriesAggregationOptions aggregation, short? year = null)
    {
        var segments = new List<string>();

        segments.Add("No location name yet");

        if (year != null)
        {
            segments.Add(year.ToString()!);
        }

        if (sss.MeasurementDefinition != null)
        {
            segments.Add(MapDataTypeToFriendlyName(sss.MeasurementDefinition.DataType));
        }
        else
        {
            segments.Add("[Missing MeasurementDefinition]");
        }

        if (year == null && binGranularity == BinGranularities.ByMonthOnly)
        {
            segments.Add(aggregation.ToString());
        }

        return string.Join(" | ", segments);
    }

    public static string MapDataTypeToFriendlyName(DataType dataType)
    {
        return dataType switch
        {
            DataType.TempMin => "Minimum temperature",
            DataType.TempMax => "Maximum temperature",
            DataType.TempMean => "Mean temperature",
            DataType.SolarRadiation => "Solar radiation",
            DataType.Precipitation => "Precipitation",
            DataType.Nino34 => "Nino 3.4",
            DataType.CO2 => "Carbon dioxide (CO\u2082)",
            DataType.CH4 => "Methane (CH\u2084)",
            DataType.N2O => "Nitrous oxide (N\u2082O)",
            DataType.IOD => "Indian Ocean Dipole (IOD)",
            DataType.Amo => "Atlantic Multidecadal Oscillation (AMO)",
            DataType.SeaIceExtent => "Sea ice extent",
            DataType.IceMeltArea => "Ice melt area",
            DataType.SunspotNumber => "Sunspot number",
            DataType.CO2Emissions => "Reported CO₂ emissions",
            DataType.ApparentTransmission => "Apparent atmospheric transmission",
            DataType.OzoneHoleArea => "Ozone Hole area",
            DataType.OzoneHoleColumn => "Ozone Hole column",
            DataType.Ozone => "Ozone",
            DataType.SeaLevel => "Sea level",
            DataType.OceanAcidity => "Ocean acidity",
            _ => throw new NotImplementedException(),
        };
    }

    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:Closing parenthesis should be spaced correctly", Justification = "Rule conflict")]
    public string FriendlyTitle(Dictionary<Guid, Location> locationDictionary)
    {
        var segments = new List<string>();

        if (SourceSeriesSpecifications!.Length == 1)
        {
            var sss = SourceSeriesSpecifications.Single();

            segments.Add(locationDictionary[sss.LocationId].Name);

            if (Year != null)
            {
                segments.Add(Year.ToString()!);
            }

            segments.Add(MapDataTypeToFriendlyName(sss.MeasurementDefinition!.DataType));

            if (sss.MeasurementDefinition.DataAdjustment != null)
            {
                segments.Add(sss.MeasurementDefinition.DataAdjustment.ToString()!);
            }
        }
        else
        {
            return GetFriendlyTitleShort();
        }

        if (SeriesTransformation != SeriesTransformations.Identity)
        {
            segments.Add("Transformation: " + GetFriendlySeriesTransformationLabel(SeriesTransformation));
        }

        if (Aggregation != SeriesAggregationOptions.Mean || (Year == null && BinGranularity == BinGranularities.ByMonthOnly))
        {
            segments.Add("Aggregation: " + Aggregation);
        }

        if (SecondaryCalculation == SecondaryCalculationOptions.AnnualChange)
        {
            segments.Add("annual change");
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
                case SeriesSmoothingOptions.MovingAverage:
                    segments.Add($"{SmoothingWindow} {(BinGranularity == BinGranularities.ByYear ? "year" : "month")} moving average");
                    break;
                case SeriesSmoothingOptions.Trendline:
                    segments.Add("Trendline");
                    break;
            }
        }

        return string.Join(" | ", segments);
    }

    public override string ToString()
    {
        return $"CSD: {BinGranularity} | {Smoothing} | {Aggregation} | {Value} | {DisplayStyle}";
    }

    public string GetFriendlyTitleShort()
    {
        return SeriesDerivationType switch
        {
            SeriesDerivationTypes.ReturnSingleSeries or SeriesDerivationTypes.AverageOfAnomaliesInRegion => BuildFriendlyTitleShortForSeries(SourceSeriesSpecifications!.Single(), BinGranularity, Aggregation, Year),
            SeriesDerivationTypes.DifferenceBetweenTwoSeries => $"[{BuildFriendlyTitleShortForSeries(SourceSeriesSpecifications![0], BinGranularity, Aggregation, Year)}] minus [{BuildFriendlyTitleShortForSeries(SourceSeriesSpecifications[1], BinGranularity, Aggregation, Year)}]",
            SeriesDerivationTypes.AverageOfMultipleSeries => BuildAverageMultipleSeriesTitle(SourceSeriesSpecifications!),
            _ => throw new NotImplementedException($"SeriesDerivationType {SeriesDerivationType}"),
        };
    }

    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:Closing parenthesis should be spaced correctly", Justification = "Rule conflict")]
    public string GetFriendlyDescription()
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
                uomLabel = Enums.UnitOfMeasureLabelShort(sss.MeasurementDefinition.UnitOfMeasure);
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

        if (SecondaryCalculation == SecondaryCalculationOptions.AnnualChange)
        {
            segments.Add("annual change");
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
                case SeriesSmoothingOptions.MovingAverage:
                    string? unit = null;

                    unit = BinGranularity switch
                    {
                        BinGranularities.ByYear => "year",
                        BinGranularities.ByYearAndDay => "day",
                        BinGranularities.ByYearAndWeek => "week",
                        BinGranularities.ByYearAndMonth => "month",
                        _ => throw new NotImplementedException($"BinGranularity {BinGranularity}"),
                    };
                    segments.Add($"{SmoothingWindow} {unit} moving average");
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

    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:Closing parenthesis should be spaced correctly", Justification = "Rule conflict")]
    private static string BuildAverageMultipleSeriesTitle(SourceSeriesSpecification[] sss)
    {
        var segments = new List<string>();

        segments.Add("No location name yet");

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

    private static string GetFriendlySeriesTransformationLabel(SeriesTransformations seriesTransformation)
    {
        return seriesTransformation switch
        {
            SeriesTransformations.IsFrosty => "Is Frost",
            SeriesTransformations.DayOfYearIfFrost => "Day if frost",
            SeriesTransformations.EqualOrAbove25 => "25°C or above",
            SeriesTransformations.EqualOrAbove35 => "35°C or above",
            SeriesTransformations.EqualOrAbove1 => "1mm or more",
            SeriesTransformations.EqualOrAbove1AndLessThan10 => "Between 1mm and 10mm",
            SeriesTransformations.EqualOrAbove10 => "10mm or more",
            SeriesTransformations.EqualOrAbove10AndLessThan25 => "Between 10mm and 25mm",
            SeriesTransformations.EqualOrAbove25mm => "25mm or more",
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

            return hashCode;
        }
    }
}
