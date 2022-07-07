using AcornSat.Core;
using AcornSat.Core.ViewModel;
using ClimateExplorer.Core.DataPreparation;
using System.Diagnostics.CodeAnalysis;
using static AcornSat.Core.Enums;

namespace AcornSat.Visualiser.UiModel
{
    public class ChartSeriesDefinition
    {
        /// <summary>
        /// Used only for uniqueness tracking by UI controls
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();


        public class SourceSeriesSpecification
        {
            public DataSetDefinitionViewModel DataSetDefinition { get; set; }
            public MeasurementDefinitionViewModel MeasurementDefinition { get; set; }
            public Guid? LocationId { get; set; }
            public string? LocationName { get; set; }
        }

        // Source data fields
        public SourceSeriesSpecification[] SourceSeriesSpecifications { get; set; }
        public SeriesDerivationTypes SeriesDerivationType { get; set; }
        public BinGranularities BinGranularity { get; set; }
        public short? Year { get; set; }

        // Data presentation fields
        public SeriesSmoothingOptions Smoothing { get; set; }
        public int SmoothingWindow { get; set; }
        public SeriesAggregationOptions Aggregation { get; set; }
        public SeriesValueOptions Value { get; set; }
        public string Colour { get; set; } // Always allocated by ColourServer; TODO: Honour RequestedColour & expose in UI
        public string RequestedColour { get; set; } // Ignored currently

        // Rendering option fields
        public SeriesDisplayStyle DisplayStyle { get; set; }
        public bool ShowTrendline { get; set; }

        // Editing mode fields

        /// <summary>
        /// If IsLocked is set, then the series will remain in place even if the user navigates to another notification
        /// </summary>
        public bool IsLocked { get; set; }

        // Transient view state
        public bool IsExpanded { get; set; }

        public override string ToString()
        {
            return $"CSD: {BinGranularity} | {Smoothing} | {Aggregation} | {Value} | {DisplayStyle}";
        }

        public string FriendlyTitle
        {
            get
            {
                List<string> segments = new List<string>();

                string s = "";

                if (Year != null) s += Year;

                if (SourceSeriesSpecifications.Length == 1)
                {
                    var sss = SourceSeriesSpecifications.Single();

                    if (sss.LocationName != null)
                    {
                        if (s.Length > 0)
                        {
                            s += " ";
                        }

                        s += sss.LocationName;
                    }

                    if (s.Length > 0) segments.Add(s);

                    if (sss.MeasurementDefinition.DataCategory != null)
                    {
                        segments.Add(sss.MeasurementDefinition.DataCategory.Value.ToString());
                    }

                    segments.Add(MapDataTypeToFriendlyName(sss.MeasurementDefinition.DataType));

                    if (sss.MeasurementDefinition.DataAdjustment != null)
                    {
                        if (sss.MeasurementDefinition.DataAdjustment != DataAdjustment.Adjusted ||
                            sss.DataSetDefinition.MeasurementDefinitions.Any(
                                x =>
                                    x != sss.MeasurementDefinition &&
                                    x.DataType == sss.MeasurementDefinition.DataType &&
                                    x.DataAdjustment != sss.MeasurementDefinition.DataAdjustment))
                        {
                            segments.Add(sss.MeasurementDefinition.DataAdjustment.ToString());
                        }
                    }
                }
                else
                {
                    segments.Add("Derived series");
                }

                switch (Smoothing)
                {
                    case SeriesSmoothingOptions.MovingAverage:
                        segments.Add(SmoothingWindow + " year moving average");
                        break;
                    case SeriesSmoothingOptions.Trendline:
                        segments.Add("Trendline");
                        break;
                }

                if (Aggregation != SeriesAggregationOptions.Mean)
                {
                    segments.Add("Aggregation: " + Aggregation);
                }

                if (Value != SeriesValueOptions.Value)
                {
                    segments.Add("Value: " + Value);
                }

                return String.Join(" | ", segments);
            }
        }

        public string GetFriendlyTitleShort()
        {
            switch (SeriesDerivationType)
            {
                case SeriesDerivationTypes.ReturnSingleSeries:
                    return BuildFriendlyTitleShortForSeries(SourceSeriesSpecifications.Single());

                case SeriesDerivationTypes.DifferenceBetweenTwoSeries:
                    return $"[{BuildFriendlyTitleShortForSeries(SourceSeriesSpecifications[0])}] minus [{BuildFriendlyTitleBuildFriendlyTitleShortForSeriesForSeries(SourceSeriesSpecifications[1])}]";

                default: throw new NotImplementedException($"SeriesDerivationType {SeriesDerivationType}");
            }            
        }

        string BuildFriendlyTitleShortForSeries(SourceSeriesSpecification sss)
        {
            List<string> segments = new List<string>();

            if (sss.LocationName != null)
            {
                segments.Add(sss.LocationName);
            }

            if (sss.MeasurementDefinition != null)
            {
                if (sss.MeasurementDefinition.DataCategory != null)
                {
                    segments.Add(sss.MeasurementDefinition.DataCategory.Value.ToString());
                }

                segments.Add(MapDataTypeToFriendlyName(sss.MeasurementDefinition.DataType));
            }
            else
            {
                segments.Add("[Missing MeasurementDefinition]");
            }

            return String.Join(" | ", segments);
        }

        public string GetFriendlyDescription()
        {
            List<string> segments = new List<string>();

            string uomLabel = null;

            if (SourceSeriesSpecifications.Length == 1)
            {
                var sss = SourceSeriesSpecifications.Single();

                if (sss.MeasurementDefinition?.DataAdjustment != null)
                {
                    if (sss.MeasurementDefinition.DataAdjustment != DataAdjustment.Adjusted ||
                        sss.DataSetDefinition.MeasurementDefinitions.Any(
                            x =>
                                x != sss.MeasurementDefinition &&
                                x.DataType == sss.MeasurementDefinition.DataType &&
                                x.DataAdjustment != sss.MeasurementDefinition.DataAdjustment))
                    {
                        segments.Add(sss.MeasurementDefinition.DataAdjustment.ToString());
                    }
                }

                if (sss.MeasurementDefinition != null)
                {
                    uomLabel = Enums.UnitOfMeasureLabelShort(sss.MeasurementDefinition.UnitOfMeasure);
                }
            }

            if (BinGranularity.IsLinear())
            {
                switch (Smoothing)
                {
                    case SeriesSmoothingOptions.MovingAverage:
                        string unit = null;

                        switch (BinGranularity)
                        {
                            case BinGranularities.ByYear: unit = "year"; break;
                            case BinGranularities.ByYearAndMonth: unit = "month"; break;
                            default: throw new NotImplementedException($"BinGranularity {BinGranularity}");
                        }
                        segments.Add($"{SmoothingWindow} {unit} moving average");
                        break;
                    case SeriesSmoothingOptions.Trendline:
                        segments.Add("Trendline");
                        break;
                }
            }

            if (Aggregation != SeriesAggregationOptions.Mean)
            {
                segments.Add("Aggregation: " + Aggregation);
            }

            if (Value != SeriesValueOptions.Value)
            {
                segments.Add("Value: " + Value);
            }

            if (uomLabel != null)
            {
                segments.Add(uomLabel);
            }

            return String.Join(" | ", segments);
        }

        string MapDataTypeToFriendlyName(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.TempMin: return "Daily minimum";
                case DataType.TempMax: return "Daily maximum";
                case DataType.SolarRadiation: return "Solar radiation";
                default: return dataType.ToString();
            }
        }

        public class ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked : IEqualityComparer<ChartSeriesDefinition>
        {
            public bool Equals(ChartSeriesDefinition? x, ChartSeriesDefinition? y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                if (x.Aggregation != y.Aggregation) return false;
                if (x.BinGranularity != y.BinGranularity) return false;
                if (x.DisplayStyle != y.DisplayStyle) return false;
                if (x.ShowTrendline != y.ShowTrendline) return false;
                if (x.Smoothing != y.Smoothing) return false;
                if (x.SmoothingWindow != y.SmoothingWindow) return false;
                if (x.Value != y.Value) return false;

                if (x.SourceSeriesSpecifications.Length != y.SourceSeriesSpecifications.Length) return false;

                for (int i = 0; i < x.SourceSeriesSpecifications.Length; i++)
                {
                    var sssX = x.SourceSeriesSpecifications[i];
                    var sssY = y.SourceSeriesSpecifications[i];

                    if (sssX.DataSetDefinition != sssY.DataSetDefinition) return false;
                    if (sssX.LocationId != sssY.LocationId) return false;
                    if (sssX.LocationName != sssY.LocationName) return false;
                    if (sssX.MeasurementDefinition != sssY.MeasurementDefinition) return false;
                }

                return true;
            }

            public int GetHashCode([DisallowNull] ChartSeriesDefinition obj)
            {
                var hashCode =
                    obj.Aggregation.GetHashCode() ^
                    obj.BinGranularity.GetHashCode() ^
                    obj.DisplayStyle.GetHashCode() ^
                    obj.ShowTrendline.GetHashCode() ^
                    obj.Smoothing.GetHashCode() ^
                    obj.SmoothingWindow.GetHashCode() ^
                    obj.Value.GetHashCode();

                for (int i = 0; i < obj.SourceSeriesSpecifications.Length; i++)
                {
                    var sss = obj.SourceSeriesSpecifications[i];

                    hashCode =
                        hashCode ^
                        sss.DataSetDefinition.Id.GetHashCode() ^
                        sss.LocationId.GetHashCode() ^
                        sss.MeasurementDefinition.DataType.GetHashCode() ^
                        sss.MeasurementDefinition.DataAdjustment.GetHashCode();
                }

                return hashCode;
            }
        }

        public class ChartSeriesDefinitionComparer : IEqualityComparer<ChartSeriesDefinition>
        {
            public bool Equals(ChartSeriesDefinition? x, ChartSeriesDefinition? y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                if (x.Aggregation != y.Aggregation) return false;
                if (x.BinGranularity != y.BinGranularity) return false;
                if (x.DisplayStyle != y.DisplayStyle) return false;
                if (x.IsLocked != y.IsLocked) return false;
                if (x.ShowTrendline != y.ShowTrendline) return false;
                if (x.Smoothing != y.Smoothing) return false;
                if (x.SmoothingWindow != y.SmoothingWindow) return false;
                if (x.Value != y.Value) return false;
                if (x.Year != y.Year) return false;

                if (x.SourceSeriesSpecifications.Length != y.SourceSeriesSpecifications.Length) return false;

                for (int i = 0; i < x.SourceSeriesSpecifications.Length; i++)
                {
                    var sssX = x.SourceSeriesSpecifications[i];
                    var sssY = y.SourceSeriesSpecifications[i];

                    if (sssX.DataSetDefinition != sssY.DataSetDefinition) return false;
                    if (sssX.LocationId != sssY.LocationId) return false;
                    if (sssX.LocationName != sssY.LocationName) return false;
                    if (sssX.MeasurementDefinition != sssY.MeasurementDefinition) return false;
                }

                return true;
            }

            public int GetHashCode([DisallowNull] ChartSeriesDefinition obj)
            {
                var hashCode =
                    obj.Aggregation.GetHashCode() ^
                    obj.BinGranularity.GetHashCode() ^
                    obj.DisplayStyle.GetHashCode() ^
                    obj.IsLocked.GetHashCode() ^
                    obj.ShowTrendline.GetHashCode() ^
                    obj.Smoothing.GetHashCode() ^
                    obj.SmoothingWindow.GetHashCode() ^
                    obj.Value.GetHashCode() ^
                    obj.Year.GetHashCode();

                for (int i = 0; i < obj.SourceSeriesSpecifications.Length; i++)
                {
                    var sss = obj.SourceSeriesSpecifications[i];

                    hashCode =
                        hashCode ^
                        sss.DataSetDefinition.Id.GetHashCode() ^
                        sss.LocationId.GetHashCode() ^
                        sss.MeasurementDefinition.DataType.GetHashCode() ^
                        sss.MeasurementDefinition.DataAdjustment.GetHashCode();
                }

                return hashCode;
            }

        }
    }
}
