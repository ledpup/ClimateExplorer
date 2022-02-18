using System;
using System.Text;
using static AcornSat.Core.Enums;

namespace AcornSat.WebApi.Model
{
    public class QueryParameters
    {
        public QueryParameters(DataType dataType, DataResolution resolution, MeasurementType measurementType, Guid locationId, Aggregation? aggregation, short? year, short? dayGrouping = 14, float? dayGroupingThreshold = .7f, short? numberOfBins = null, float? binSize = null, short? completeYearThreshold = 350)
        {
            DataType = dataType;
            Resolution = resolution;
            MeasurementType = measurementType;
            LocationId = locationId;
            Aggregation = aggregation;
            Year = year;
            switch (Aggregation)
            {
                case Core.Enums.Aggregation.GroupThenAverage:
                case Core.Enums.Aggregation.GroupThenAverage_Relative:
                    AggregationParameters = new GroupThenAverage(dayGrouping.Value, dayGroupingThreshold.Value);
                    break;
                case Core.Enums.Aggregation.BinThenCount:
                    AggregationParameters = new BinThenCount(completeYearThreshold.Value, numberOfBins, binSize);
                    break;
            }
        }
        public DataType DataType { get; set; }
        public DataResolution Resolution { get; set; }
        public MeasurementType MeasurementType { get; set; }
        public Guid LocationId { get; set; }
        public Aggregation? Aggregation { get; set; }
        public short? Year { get; set; }

        public AggregationParameters AggregationParameters { get; set; }

        public string ToBase64String()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append($"{DataType}_{Resolution}_{MeasurementType}_{LocationId}");
            if (Aggregation.HasValue)
            {
                stringBuilder.Append($"_{Aggregation}");
                switch (Aggregation)
                {
                    case Core.Enums.Aggregation.GroupThenAverage:
                    case Core.Enums.Aggregation.GroupThenAverage_Relative:
                        stringBuilder.Append($"_{((GroupThenAverage)AggregationParameters).DayGrouping}");
                        stringBuilder.Append($"_{((GroupThenAverage)AggregationParameters).DayGroupingThreshold}");
                        break;
                    case Core.Enums.Aggregation.BinThenCount:
                        stringBuilder.Append($"_{((BinThenCount)AggregationParameters).SufficientNumberOfDaysInYearThreshold}");
                        if (((BinThenCount)AggregationParameters).NumberOfBins.HasValue)
                        {
                            stringBuilder.Append($"_{((BinThenCount)AggregationParameters).NumberOfBins}");
                        }
                        if (((BinThenCount)AggregationParameters).BinSize.HasValue)
                        {
                            stringBuilder.Append($"_{((BinThenCount)AggregationParameters).BinSize}");
                        }
                        break;
                }
            }
            if (Year.HasValue)
            {
                stringBuilder.Append($"_{Year}");
            }
            
            string encodedStr = Convert.ToBase64String(Encoding.UTF8.GetBytes(stringBuilder.ToString()));

            return encodedStr;
        }
    }
}
