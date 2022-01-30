using System;
using System.Text;
using static AcornSat.Core.Enums;

namespace AcornSat.WebApi.Model
{
    public class QueryParameters
    {
        public QueryParameters(DataType dataType, DataResolution resolution, MeasurementType measurementType, Guid locationId, StatisticalMethod? statisticalMethod, short? year, short? dayGrouping = 14, float? dayGroupingThreshold = .7f, short? numberOfBins = null, short? binSize = null)
        {
            DataType = dataType;
            Resolution = resolution;
            MeasurementType = measurementType;
            LocationId = locationId;
            StatisticalMethod = statisticalMethod;
            Year = year;
            switch (StatisticalMethod)
            {
                case Core.Enums.StatisticalMethod.GroupByDayThenAverage:
                case Core.Enums.StatisticalMethod.GroupByDayThenAverage_Relative:
                    StatsParameters = new GroupThenAverage(dayGrouping.Value, dayGroupingThreshold.Value);
                    break;
                case Core.Enums.StatisticalMethod.BinThenCount:
                    StatsParameters = new BinThenCount(numberOfBins, binSize);
                    break;
            }
        }
        public DataType DataType { get; set; }
        public DataResolution Resolution { get; set; }
        public MeasurementType MeasurementType { get; set; }
        public Guid LocationId { get; set; }
        public StatisticalMethod? StatisticalMethod { get; set; }
        public short? Year { get; set; }

        public StatsParameters StatsParameters { get; set; }

        public string ToBase64String()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append($"{DataType}_{Resolution}_{MeasurementType}_{LocationId}");
            if (StatisticalMethod.HasValue)
            {
                stringBuilder.Append($"_{StatisticalMethod}");
                switch (StatisticalMethod)
                {
                    case Core.Enums.StatisticalMethod.GroupByDayThenAverage:
                    case Core.Enums.StatisticalMethod.GroupByDayThenAverage_Relative:
                        stringBuilder.Append($"_{((GroupThenAverage)StatsParameters).DayGrouping}");
                        stringBuilder.Append($"_{((GroupThenAverage)StatsParameters).DayGroupingThreshold}");
                        break;
                    case Core.Enums.StatisticalMethod.BinThenCount:
                        if (((BinThenCount)StatsParameters).NumberOfBins.HasValue)
                        {
                            stringBuilder.Append($"_{((BinThenCount)StatsParameters).NumberOfBins}");
                        }
                        if (((BinThenCount)StatsParameters).BinSize.HasValue)
                        {
                            stringBuilder.Append($"_{((BinThenCount)StatsParameters).BinSize}");
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
