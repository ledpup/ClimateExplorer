using AcornSat.Core;
using AcornSat.Core.ViewModel;
using System.Diagnostics.CodeAnalysis;
using static AcornSat.Core.Enums;

namespace AcornSat.Visualiser.UiModel
{
    public class ChartSeriesDefinition
    {
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

        public string FriendlyTitleShort
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

                return String.Join(" | ", segments);
            }
        }

        public string FriendlyDescription
        {
            get
            {
                List<string> segments = new List<string>();

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

                segments.Add(Enums.UnitOfMeasureLabelShort(MeasurementDefinition.UnitOfMeasure));

                return String.Join(" | ", segments);
            }
        }

        private string MapDataTypeToFriendlyName(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.TempMin: return "Daily minimum";
                case DataType.TempMax: return "Daily maximum";
                default: return dataType.ToString();    
            }    
        }

        // Source data fields
        public DataSetDefinitionViewModel DataSetDefinition { get; set; }
        public MeasurementDefinitionViewModel MeasurementDefinition { get; set; }
        public DataResolution DataResolution { get; set; }
        public short? Year { get; set; }
        public Guid? LocationId { get; set; }
        public string? LocationName { get; set; }

        // Data presentation fields
        public SeriesSmoothingOptions Smoothing { get; set; }
        public int SmoothingWindow { get; set; }
        public SeriesAggregationOptions Aggregation { get; set; }
        public SeriesValueOptions Value { get; set; }
        public string Colour { get; set; }

        // Rendering option fields
        public SeriesDisplayStyle DisplayStyle { get; set; }
        public bool ShowTrendline { get; set; }

        // Editing mode fields

        /// <summary>
        /// If IsLocked is set, then the series will remain in place even if the user navigates to another notification
        /// </summary>
        public bool IsLocked { get; set; }

        public override string ToString()
        {
            return $"CSD: {DataSetDefinition.Name} | {MeasurementDefinition.DataType} {MeasurementDefinition.DataAdjustment} | {DataResolution} | {LocationName} | {Smoothing} | {Aggregation} | {Value} | {DisplayStyle}";
        }

        public class ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked : IEqualityComparer<ChartSeriesDefinition>
        {
            public bool Equals(ChartSeriesDefinition? x, ChartSeriesDefinition? y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                if (x.Aggregation != y.Aggregation) return false;
                if (x.DataResolution != y.DataResolution) return false;
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
                    obj.DataResolution.GetHashCode() ^
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
                if (x.DataResolution != y.DataResolution) return false;
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
                    obj.DataResolution.GetHashCode() ^
                    obj.DataSetDefinition.Id.GetHashCode() ^
                    obj.DisplayStyle.GetHashCode() ^
                    obj.IsLocked.GetHashCode() ^
                    obj.LocationId.GetHashCode() ^
                    obj.LocationName.GetHashCode() ^
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
