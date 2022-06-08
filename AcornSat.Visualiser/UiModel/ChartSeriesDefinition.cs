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

                if (Year != null) s += Year + " ";

                s += MeasurementDefinition.DataType;

                segments.Add(s);
                
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

        // Source data fields
        public DataSetDefinitionViewModel DataSetDefinition { get; set; }
        public MeasurementDefinitionViewModel MeasurementDefinition { get; set; }
        public DataResolution DataResolution { get; set; }
        public short? Year { get; set; }
        public Guid? LocationId { get; set; }

        // Data presentation fields
        public SeriesSmoothingOptions Smoothing { get; set; }
        public int SmoothingWindow { get; set; }
        public SeriesAggregationOptions Aggregation { get; set; }
        public SeriesValueOptions Value { get; set; }
    }
}
