namespace ClimateExplorer.Core;

public static class YearlyDifferenceCalculator
{
    public static IEnumerable<float?> CalculateYearlyDifference(this IEnumerable<float?> values)
    {
        var valuesArray = values as float?[] ?? values.ToArray();

        // Set the first year slot to be null
        var result = new List<float?>
        {
            null
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
