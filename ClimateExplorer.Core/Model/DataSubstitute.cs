using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Core.Model;

public class DataSubstitute
{
    public DataType DataType { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }

    public static List<DataSubstitute> StandardTemperatureDataMatches()
    {
        var dataMatches = new List<DataSubstitute>
        {
            new DataSubstitute
            {
                DataType = DataType.TempMax,
                DataAdjustment = Enums.DataAdjustment.Adjusted,
            },
            new DataSubstitute
            {
                DataType = DataType.TempMean,
                DataAdjustment = Enums.DataAdjustment.Adjusted,
            },
            new DataSubstitute
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
            new DataSubstitute
            {
                DataType = DataType.TempMax,
                DataAdjustment = Enums.DataAdjustment.Adjusted,
            },
            new DataSubstitute
            {
                DataType = DataType.TempMean,
                DataAdjustment = Enums.DataAdjustment.Adjusted,
            },
        };
        return dataMatches;
    }

    public static List<DataSubstitute> UnadjustedTemperatureDataMatches()
    {
        var dataMatches = new List<DataSubstitute>
        {
            new DataSubstitute
            {
                DataType = DataType.TempMax,
                DataAdjustment = Enums.DataAdjustment.Unadjusted,
            },
            new DataSubstitute
            {
                DataType = DataType.TempMean,
                DataAdjustment = Enums.DataAdjustment.Unadjusted,
            },
        };
        return dataMatches;
    }
}
