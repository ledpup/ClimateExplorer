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
                string s = "";

                if (Year != null) s += Year + " ";

                s += MeasurementDefinition.DataType + " " + MeasurementDefinition.DataAdjustment + " | ";

                switch (Smoothing)
                {
                    case SeriesSmoothingOptions.None:
                        s += "No smoothing";
                        break;
                    case SeriesSmoothingOptions.MovingAverage:
                        s += SmoothingWindow + " year moving average";
                        break;
                    case SeriesSmoothingOptions.Trendline:
                        s += "Trendline";
                        break;
                }

                s += " | Aggregation: " + Aggregation;
                s += " | Value: " + Value;
                return s;
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
        public int? SmoothingWindow { get; set; }
        public SeriesAggregationOptions Aggregation { get; set; }
        public SeriesValueOptions Value { get; set; }
    }
}
