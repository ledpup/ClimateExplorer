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

        public string FriendlyTitle
        {
            get
            {
                List<string> segments = new List<string>();

                string s = "";

                if (Year != null) s += Year;

                if (LocationName != null)
                {
                    if (s.Length > 0)
                    {
                        s += " ";
                    }

                    s += LocationName;
                }

                if (s.Length > 0) segments.Add(s);

                if (MeasurementDefinition.DataCategory != null)
                {
                    segments.Add(MeasurementDefinition.DataCategory.Value.ToString());
                }

                segments.Add(MapDataTypeToFriendlyName(MeasurementDefinition.DataType));

                if (MeasurementDefinition.DataAdjustment != null)
                {
                    if (MeasurementDefinition.DataAdjustment != DataAdjustment.Adjusted ||
                        DataSetDefinition.MeasurementDefinitions.Any(
                            x =>
                                x != MeasurementDefinition &&
                                x.DataType == MeasurementDefinition.DataType &&
                                x.DataAdjustment != MeasurementDefinition.DataAdjustment))
                    {
                        segments.Add(MeasurementDefinition.DataAdjustment.ToString());
                    }
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
            List<string> segments = new List<string>();

            string s = "";

            if (Year != null) s += Year;

            if (LocationName != null)
            {
                if (s.Length > 0)
                {
                    s += " ";
                }

                s += LocationName;
            }

            if (s.Length > 0) segments.Add(s);

            if (MeasurementDefinition != null)
            {
                if (MeasurementDefinition.DataCategory != null)
                {
                    segments.Add(MeasurementDefinition.DataCategory.Value.ToString());
                }

                segments.Add(MapDataTypeToFriendlyName(MeasurementDefinition.DataType));
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

            if (MeasurementDefinition?.DataAdjustment != null)
            {
                if (MeasurementDefinition.DataAdjustment != DataAdjustment.Adjusted ||
                    DataSetDefinition.MeasurementDefinitions.Any(
                        x =>
                            x != MeasurementDefinition &&
                            x.DataType == MeasurementDefinition.DataType &&
                            x.DataAdjustment != MeasurementDefinition.DataAdjustment))
                {
                    segments.Add(MeasurementDefinition.DataAdjustment.ToString());
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

            if (MeasurementDefinition != null)
            {
                segments.Add(Enums.UnitOfMeasureLabelShort(MeasurementDefinition.UnitOfMeasure));
            }

            return String.Join(" | ", segments);
        }

        private string MapDataTypeToFriendlyName(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.TempMin: return "Daily minimum";
                case DataType.TempMax: return "Daily maximum";
                case DataType.SolarRadiation: return "Solar radiation";
                default: return dataType.ToString();    
            }    
        }

        // Source data fields
        public DataSetDefinitionViewModel DataSetDefinition { get; set; }
        public MeasurementDefinitionViewModel MeasurementDefinition { get; set; }
        public BinGranularities BinGranularity { get; set; }
        public short? Year { get; set; }
        public Guid? LocationId { get; set; }
        public string? LocationName { get; set; }

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
            return $"CSD: {DataSetDefinition.Name} | {MeasurementDefinition?.DataType} {MeasurementDefinition?.DataAdjustment} | {BinGranularity} | {LocationName} | {Smoothing} | {Aggregation} | {Value} | {DisplayStyle}";
        }

        public class ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked : IEqualityComparer<ChartSeriesDefinition>
        {
            public bool Equals(ChartSeriesDefinition? x, ChartSeriesDefinition? y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                if (x.Aggregation != y.Aggregation) return false;
                if (x.BinGranularity != y.BinGranularity) return false;
                if (x.DataSetDefinition != y.DataSetDefinition) return false;
                if (x.DisplayStyle != y.DisplayStyle) return false;
                if (x.LocationId != y.LocationId) return false;
                if (x.LocationName != y.LocationName) return false;
                if (x.MeasurementDefinition != y.MeasurementDefinition) return false;
                if (x.ShowTrendline != y.ShowTrendline) return false;
                if (x.Smoothing != y.Smoothing) return false;
                if (x.SmoothingWindow != y.SmoothingWindow) return false;
                if (x.Value != y.Value) return false;

                return true;
            }

            public int GetHashCode([DisallowNull] ChartSeriesDefinition obj)
            {
                return
                    obj.Aggregation.GetHashCode() ^
                    obj.BinGranularity.GetHashCode() ^
                    obj.DataSetDefinition.Id.GetHashCode() ^
                    obj.DisplayStyle.GetHashCode() ^
                    obj.LocationId.GetHashCode() ^
                    obj.LocationName.GetHashCode() ^
                    obj.MeasurementDefinition.GetHashCode() ^
                    obj.ShowTrendline.GetHashCode() ^
                    obj.Smoothing.GetHashCode() ^
                    obj.SmoothingWindow.GetHashCode() ^
                    obj.Value.GetHashCode();
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
                if (x.DataSetDefinition != y.DataSetDefinition) return false;
                if (x.DisplayStyle != y.DisplayStyle) return false;
                if (x.IsLocked != y.IsLocked) return false;
                if (x.LocationId != y.LocationId) return false;
                if (x.LocationName != y.LocationName) return false;
                if (x.MeasurementDefinition != y.MeasurementDefinition) return false;
                if (x.ShowTrendline != y.ShowTrendline) return false;
                if (x.Smoothing != y.Smoothing) return false;
                if (x.SmoothingWindow != y.SmoothingWindow) return false;
                if (x.Value != y.Value) return false;
                if (x.Year != y.Year) return false;

                return true;
            }

            public int GetHashCode([DisallowNull] ChartSeriesDefinition obj)
            {
                return
                    obj.Aggregation.GetHashCode() ^
                    obj.BinGranularity.GetHashCode() ^
                    obj.DataSetDefinition.Id.GetHashCode() ^
                    obj.DisplayStyle.GetHashCode() ^
                    obj.IsLocked.GetHashCode() ^
                    obj.LocationId?.GetHashCode() ?? 0 ^
                    obj.LocationName?.GetHashCode() ?? 0 ^
                    obj.MeasurementDefinition.GetHashCode() ^
                    obj.ShowTrendline.GetHashCode() ^
                    obj.Smoothing.GetHashCode() ^
                    obj.SmoothingWindow.GetHashCode() ^
                    obj.Value.GetHashCode() ^
                    obj.Year.GetHashCode();
            }
        }
    }
}
