namespace ClimateExplorer.WebApi.Model;

using System;
using System.Text;
using static ClimateExplorer.Core.Enums;

public class QueryParameters
{
    public QueryParameters(DataType dataType, DataResolution resolution, DataAdjustment? dataAdjustment, Guid? locationId, AggregationMethod? aggregationMethod, short? year, short? dayGrouping = 14, float? dayGroupingThreshold = .7f, short? numberOfBins = null, short? binSize = null)
    {
        DataType = dataType;
        Resolution = resolution;
        DataAdjustment = dataAdjustment;
        LocationId = locationId;
        AggregationMethod = aggregationMethod;
        Year = year;
        switch (AggregationMethod)
        {
            case Core.Enums.AggregationMethod.GroupByDayThenAverage:
            case Core.Enums.AggregationMethod.GroupByDayThenAverage_Anomaly:
            case Core.Enums.AggregationMethod.Sum:
                StatsParameters = new GroupThenAverage(dayGrouping.Value, dayGroupingThreshold.Value);
                break;
            case Core.Enums.AggregationMethod.BinThenCount:
                StatsParameters = new BinThenCount(numberOfBins, binSize);
                break;
        }
    }

    public DataType DataType { get; set; }
    public DataResolution Resolution { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public Guid? LocationId { get; set; }
    public AggregationMethod? AggregationMethod { get; set; }
    public short? Year { get; set; }

    public StatsParameters StatsParameters { get; set; }

    public string ToBase64String()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append($"{DataType}_{Resolution}");
        if (DataAdjustment.HasValue)
        {
            stringBuilder.Append($"_{DataAdjustment}");
        }

        if (LocationId.HasValue)
        {
            stringBuilder.Append($"_{LocationId}");
        }

        if (AggregationMethod.HasValue)
        {
            stringBuilder.Append($"_{AggregationMethod}");
            switch (AggregationMethod)
            {
                case Core.Enums.AggregationMethod.GroupByDayThenAverage:
                case Core.Enums.AggregationMethod.GroupByDayThenAverage_Anomaly:
                    stringBuilder.Append($"_{((GroupThenAverage)StatsParameters).DayGrouping}");
                    stringBuilder.Append($"_{((GroupThenAverage)StatsParameters).DayGroupingThreshold}");
                    break;
                case Core.Enums.AggregationMethod.BinThenCount:
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
