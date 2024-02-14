namespace ClimateExplorer.Core;

public static class YearlyDifferenceCalculator
{
    public static IEnumerable<double?> CalculateYearlyDifference(this IEnumerable<double?> values)
    {
        var valuesArray = values as double?[] ?? values.ToArray();

        // Set the first year slot to be null
        var result = new List<double?>
        {
            null,
        };

        for (int i = 1; i < valuesArray.Length; i++)
        {
            if (valuesArray[i] == null || valuesArray[i - 1] == null)
            {
                result.Add(null);
                continue;
            }

            result.Add(valuesArray[i] - valuesArray[i - 1]);
        }

        return result;
    }
}
