using System;
using System.Text;
using static AcornSat.Core.Enums;

namespace AcornSat.WebApi.Model
{
    public class QueryParameters
    {
        public QueryParameters(DataType dataType, DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year, short? dayGrouping, float? dayGroupingThreshold, bool? relativeToAverage)
        {
            DataType = dataType;
            Resolution = resolution;
            MeasurementType = measurementType;
            LocationId = locationId;
            Year = year;
            DayGrouping = dayGrouping;
            DayGroupingThreshold = dayGroupingThreshold;
            RelativeToAverage = relativeToAverage;
        }
        public DataType DataType { get; set; }
        public DataResolution Resolution { get; set; }
        public MeasurementType MeasurementType { get; set; }
        public Guid LocationId { get; set; }
        public short? Year { get; set; }
        public short? DayGrouping { get; set; }
        public float? DayGroupingThreshold { get; set; }
        public bool? RelativeToAverage { get; set; }

        public string ToBase64String()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append($"{DataType}_{Resolution}_{MeasurementType}_{LocationId}");
            if (Year.HasValue)
            {
                stringBuilder.Append($"_{Year}");
            }
            if (DayGrouping.HasValue)
            {
                stringBuilder.Append($"_{DayGrouping}");
            }
            if (DayGroupingThreshold.HasValue)
            {
                stringBuilder.Append($"_{DayGroupingThreshold}");
            }
            if (RelativeToAverage.HasValue)
            {
                stringBuilder.Append($"_{RelativeToAverage}");
            }
            
            string encodedStr = Convert.ToBase64String(Encoding.UTF8.GetBytes(stringBuilder.ToString()));

            return encodedStr;
        }
    }
}
