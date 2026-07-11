namespace ClimateExplorer.Core.Stats;

using static MathHelpers;

public static class StandardDeviation
{
    public static double? PopulationStandardDeviation(IEnumerable<double> values)
    {
        var list = values.ToList();

        if (list.Count == 0)
        {
            return null;
        }

        var mean = list.Average();

        var variance = list
            .Average(value => Square(value - mean));

        return Math.Sqrt(variance);
    }

    public static double? SampleStandardDeviation(IEnumerable<double> values)
    {
        var list = values.ToList();

        if (list.Count < 2)
        {
            return null;
        }

        var mean = list.Average();

        var variance = list
            .Sum(value => Square(value - mean)) / (list.Count - 1);

        return Math.Sqrt(variance);
    }

    public static double? StandardDeviationsFromMean(
        double value,
        IEnumerable<double> historicalValues)
    {
        var list = historicalValues.ToList();

        if (list.Count == 0)
        {
            return null;
        }

        var mean = list.Average();

        var variance = list
            .Average(historicalValue => Square(historicalValue - mean));
        var standardDeviation = Math.Sqrt(variance);

        if (standardDeviation == 0)
        {
            return null;
        }

        return (value - mean) / standardDeviation;
    }
}
