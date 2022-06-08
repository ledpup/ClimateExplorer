using AcornSat.Core.ViewModel;
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
                
                if (MeasurementDefinition.DataAdjustment != DataAdjustment.Adjusted ||
                    DataSetDefinition.MeasurementDefinitions.Any(
                        x =>
                            x != MeasurementDefinition &&
                            x.DataType == MeasurementDefinition.DataType &&
                            x.DataAdjustment != MeasurementDefinition.DataAdjustment))
                {
                    segments.Add(MeasurementDefinition.DataAdjustment.ToString());
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

        // Rendering option fields
        public SeriesDisplayStyle DisplayStyle { get; set; }

        // Editing mode fields

        /// <summary>
        /// If IsLocked is set, then the series will remain in place even if the user navigates to another notification
        /// </summary>
        public bool IsLocked { get; set; }
    }
}
