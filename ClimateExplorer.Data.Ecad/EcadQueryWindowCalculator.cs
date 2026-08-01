namespace ClimateExplorer.Data.Ecad;

/// <summary>
/// Splits a date range into windows the API will accept. The server caps a query at
/// <see cref="EcadConstants.MaximumDataPointsPerQuery"/> data points, counted as
/// <c>timePoints * parameterCount * stationCount</c>, and rejects anything larger with an HTTP 413
/// that states the arithmetic. Bootstrapping a station's full history crosses that cap, so the range
/// has to be walked in pieces.
/// </summary>
public static class EcadQueryWindowCalculator
{
    public static int GetMaximumDaysPerQuery(int parameterCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parameterCount);

        var billedParameters = parameterCount * EcadConstants.ResponseParametersPerRequestedParameter;
        var days = EcadConstants.MaximumDataPointsPerQuery / billedParameters;
        return days > 0
            ? days
            : throw new InvalidOperationException(
                $"A query for {parameterCount} parameter(s) cannot fit inside the API's {EcadConstants.MaximumDataPointsPerQuery} data point limit.");
    }

    public static IEnumerable<(DateOnly From, DateOnly To)> GetWindows(DateOnly from, DateOnly to, int parameterCount)
    {
        if (from > to)
        {
            yield break;
        }

        var maximumDays = GetMaximumDaysPerQuery(parameterCount);
        var windowStart = from;
        while (windowStart <= to)
        {
            // The range is inclusive at both ends, so a window of n days ends n-1 days after it starts.
            var windowEnd = windowStart.AddDays(maximumDays - 1);
            if (windowEnd > to)
            {
                windowEnd = to;
            }

            yield return (windowStart, windowEnd);
            windowStart = windowEnd.AddDays(1);
        }
    }
}
