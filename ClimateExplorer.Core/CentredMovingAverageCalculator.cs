namespace ClimateExplorer.Core;

public static class CentredMovingAverageCalculator
{
    public static IEnumerable<float?> CalculateCentredMovingAverage(this IEnumerable<float?> values, int windowSize, float requiredDataThreshold)
    {
        float?[] valuesArray = values as float?[] ?? values.ToArray();

        List<float?> result = new List<float?>();

        int startIndex = 0 - windowSize / 2;
        int endIndex = windowSize / 2;

        for (int i = 0; i < valuesArray.Length; i++, startIndex++, endIndex++)
        {
            if (startIndex < 0 || endIndex >= valuesArray.Length)
            {
                result.Add(null);
                continue;
            }

            var window = valuesArray.Skip(startIndex).Take(windowSize).ToArray();

            var countOfValuesInWindowWithValue = window.Count(x => x.HasValue);
            var proportionOfDataPresentInWindow = countOfValuesInWindowWithValue / (float)windowSize;

            if (proportionOfDataPresentInWindow >= requiredDataThreshold)
            {
                result.Add(window.Average());
            }
            else
            {
                result.Add(null);
            }
        }

        return result;
    }
}
