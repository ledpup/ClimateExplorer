namespace ClimateExplorer.Core.Stats;

public static class CentredMovingAverageCalculator
{
    public static IEnumerable<double?> CalculateCentredMovingAverage(this IEnumerable<double?> values, int windowSize, float requiredDataThreshold)
    {
        double?[] valuesArray = values as double?[] ?? values.ToArray();

        List<double?> result = [];

        // An even window can't be centred on a point, so use a 2xN moving average: the mean of the
        // two adjacent N-point windows. That spans N + 1 points with the two end points at half
        // weight (e.g. 0.5, 1, 1, 1, 0.5 for N = 4). Odd windows span N points, all at full weight.
        bool isEvenWindow = windowSize % 2 == 0;
        double endPointWeight = isEvenWindow ? 0.5 : 1;

        int startIndex = 0 - (windowSize / 2);
        int endIndex = windowSize / 2;

        for (int i = 0; i < valuesArray.Length; i++, startIndex++, endIndex++)
        {
            if (startIndex < 0 || endIndex >= valuesArray.Length)
            {
                result.Add(null);
                continue;
            }

            double weightedSum = 0;
            double weightOfValuesInWindowWithValue = 0;

            for (int j = startIndex; j <= endIndex; j++)
            {
                var value = valuesArray[j];

                if (!value.HasValue)
                {
                    continue;
                }

                var weight = j == startIndex || j == endIndex ? endPointWeight : 1;

                weightedSum += value.Value * weight;
                weightOfValuesInWindowWithValue += weight;
            }

            // Weights sum to windowSize for both odd and even windows
            var proportionOfDataPresentInWindow = (float)weightOfValuesInWindowWithValue / windowSize;

            if (weightOfValuesInWindowWithValue > 0 && proportionOfDataPresentInWindow >= requiredDataThreshold)
            {
                result.Add(weightedSum / weightOfValuesInWindowWithValue);
            }
            else
            {
                result.Add(null);
            }
        }

        return result;
    }
}
