namespace ClimateExplorer.Core.Model;

using static ClimateExplorer.Core.Enums;

public class DataSubstitute
{
    public DataType DataType { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public DataResolution? DataResolution { get; set; }

    public static List<DataSubstitute> StandardTemperatureDataMatches()
    {
        var dataMatches = new List<DataSubstitute>
        {
            new ()
            {
                DataType = DataType.TempMean,
                DataAdjustment = Enums.DataAdjustment.Adjusted,
            },
            new ()
            {
                DataType = DataType.TempMax,
                DataAdjustment = Enums.DataAdjustment.Adjusted,
            },
            new ()
            {
                DataType = DataType.TempMax,
                DataAdjustment = Enums.DataAdjustment.Unadjusted,
            },
        };
        return dataMatches;
    }

    public static List<DataSubstitute> AdjustedTemperatureDataMatches()
    {
        var dataMatches = new List<DataSubstitute>
        {
            new ()
            {
                DataType = DataType.TempMean,
                DataAdjustment = Enums.DataAdjustment.Adjusted,
            },
            new ()
            {
                DataType = DataType.TempMax,
                DataAdjustment = Enums.DataAdjustment.Adjusted,
            },
        };
        return dataMatches;
    }

    public static List<DataSubstitute> UnadjustedTemperatureDataMatches()
    {
        var dataMatches = new List<DataSubstitute>
        {
            new ()
            {
                DataType = DataType.TempMean,
                DataAdjustment = Enums.DataAdjustment.Unadjusted,
            },
            new ()
            {
                DataType = DataType.TempMax,
                DataAdjustment = Enums.DataAdjustment.Unadjusted,
            },
        };
        return dataMatches;
    }

    public static List<DataSubstitute> DailyMaxTemperatureDataMatches()
    {
        var dataMatches = new List<DataSubstitute>
        {
            new ()
            {
                DataType = DataType.TempMax,
                DataAdjustment = Enums.DataAdjustment.Adjusted,
                DataResolution = Enums.DataResolution.Daily,
            },
            new ()
            {
                DataType = DataType.TempMax,
                DataAdjustment = Enums.DataAdjustment.Unadjusted,
                DataResolution = Enums.DataResolution.Daily,
            },
        };
        return dataMatches;
    }

    public static List<DataSubstitute> DailyMinTemperatureDataMatches()
    {
        var dataMatches = new List<DataSubstitute>
        {
            new ()
            {
                DataType = DataType.TempMin,
                DataAdjustment = Enums.DataAdjustment.Adjusted,
                DataResolution = Enums.DataResolution.Daily,
            },
            new ()
            {
                DataType = DataType.TempMin,
                DataAdjustment = Enums.DataAdjustment.Unadjusted,
                DataResolution = Enums.DataResolution.Daily,
            },
        };
        return dataMatches;
    }
}
