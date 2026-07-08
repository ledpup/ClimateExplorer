namespace ClimateExplorer.Core.Stats;

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
            .Average(value => (value - mean) * (value - mean));

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
            .Sum(value => (value - mean) * (value - mean)) / (list.Count - 1);

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
            .Average(historicalValue => (historicalValue - mean) * (historicalValue - mean));
        var standardDeviation = Math.Sqrt(variance);

        if (standardDeviation == 0)
        {
            return null;
        }

        return (value - mean) / standardDeviation;
    }
}
