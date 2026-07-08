namespace ClimateExplorer.Core.Calculators;

public static class RecentObservationComparison
{
    private const double Tolerance = 0.0000001d;

    public static RecentObservationComparisonResult? Rank(double value, IEnumerable<double?> historicalValues)
    {
        var values = historicalValues
            .Where(x => x.HasValue && double.IsFinite(x.Value))
            .Select(x => x!.Value)
            .ToList();

        if (values.Count == 0 || !double.IsFinite(value))
        {
            return null;
        }

        var lowerCount = values.Count(x => x < value - Tolerance);
        var higherCount = values.Count(x => x > value + Tolerance);
        var highPercentile = lowerCount / (double)(values.Count + 1) * 100d;
        var lowPercentile = higherCount / (double)(values.Count + 1) * 100d;
        var max = values.Max();
        var min = values.Min();
        var average = values.Average();

        return new RecentObservationComparisonResult
        {
            Value = value,
            HistoricalCount = values.Count,
            HighRank = higherCount + 1,
            LowRank = lowerCount + 1,
            HighPercentile = highPercentile,
            LowPercentile = lowPercentile,
            HistoricalAverage = average,
            HistoricalMax = max,
            HistoricalMin = min,
            Anomaly = value - average,
            IsNewHighRecord = value > max + Tolerance,
            IsNewLowRecord = value < min - Tolerance,
            IsTiedHighRecord = Math.Abs(value - max) <= Tolerance,
            IsTiedLowRecord = Math.Abs(value - min) <= Tolerance,
            Direction = GetDirection(highPercentile, lowPercentile),
        };
    }

    public static string BuildTemperatureHeadline(string comparisonLabel, RecentObservationComparisonResult ranking)
    {
        if (ranking.IsNewHighRecord)
        {
            return $"Warmest {comparisonLabel}";
        }

        if (ranking.IsNewLowRecord)
        {
            return $"Coolest {comparisonLabel}";
        }

        if (ranking.IsTiedHighRecord && ranking.HighRank == 1)
        {
            return $"Equal warmest {comparisonLabel}";
        }

        if (ranking.IsTiedLowRecord && ranking.LowRank == 1)
        {
            return $"Equal coolest {comparisonLabel}";
        }

        if (ranking.Direction == RecentObservationComparisonDirection.High)
        {
            if (ranking.HighRank <= 5 && !ranking.IsTiedHighRecord)
            {
                return $"{FormatOrdinal(ranking.HighRank)} warmest {comparisonLabel}";
            }

            if (ranking.HighPercentile >= 95d)
            {
                return "Top 5% warmest";
            }

            if (ranking.HighPercentile >= 90d)
            {
                return "Top 10% warmest";
            }

            return ranking.HighPercentile >= 75d ? "Warmer than usual" : "Slightly warmer than average";
        }

        if (ranking.Direction == RecentObservationComparisonDirection.Low)
        {
            if (ranking.LowRank <= 5 && !ranking.IsTiedLowRecord)
            {
                return $"{FormatOrdinal(ranking.LowRank)} coolest {comparisonLabel}";
            }

            if (ranking.LowPercentile >= 95d)
            {
                return "Top 5% coolest";
            }

            if (ranking.LowPercentile >= 90d)
            {
                return "Top 10% coolest";
            }

            return ranking.LowPercentile >= 75d ? "Cooler than usual" : "Slightly cooler than average";
        }

        return "Near average";
    }

    public static string BuildPrecipitationHeadline(string comparisonLabel, RecentObservationComparisonResult ranking)
    {
        if (ranking.IsNewHighRecord)
        {
            return $"Wettest {comparisonLabel}";
        }

        if (ranking.IsNewLowRecord)
        {
            return $"Driest {comparisonLabel}";
        }

        if (ranking.Direction == RecentObservationComparisonDirection.High)
        {
            if (ranking.HighRank <= 5 && !ranking.IsTiedHighRecord)
            {
                return $"{FormatOrdinal(ranking.HighRank)} wettest {comparisonLabel}";
            }

            if (ranking.HighPercentile >= 95d)
            {
                return "Top 5% wettest";
            }

            if (ranking.HighPercentile >= 90d)
            {
                return "Top 10% wettest";
            }

            return ranking.HighPercentile >= 70d ? "Wetter than usual" : "Slightly wetter than average";
        }

        if (ranking.Direction == RecentObservationComparisonDirection.Low)
        {
            if (ranking.LowRank <= 5 && !ranking.IsTiedLowRecord)
            {
                return $"{FormatOrdinal(ranking.LowRank)} driest {comparisonLabel}";
            }

            if (ranking.LowPercentile >= 95d)
            {
                return "Top 5% driest";
            }

            if (ranking.LowPercentile >= 90d)
            {
                return "Top 10% driest";
            }

            return ranking.LowPercentile >= 70d ? "Drier than usual" : "Slightly drier than average";
        }

        return "Near average rainfall";
    }

    public static string BuildTemperaturePercentileSentence(string comparisonLabelPlural, int? startYear, RecentObservationComparisonResult ranking)
    {
        var direction = ranking.Direction == RecentObservationComparisonDirection.Low ? "Cooler" : "Warmer";
        var percentile = ranking.Direction == RecentObservationComparisonDirection.Low ? ranking.LowPercentile : ranking.HighPercentile;
        return $"{direction} than {FormatPercent(percentile)}% of {comparisonLabelPlural}";
    }

    public static string BuildPrecipitationPercentileSentence(int? startYear, RecentObservationComparisonResult ranking)
    {
        var direction = ranking.Direction == RecentObservationComparisonDirection.Low ? "Drier" : "Wetter";
        var percentile = ranking.Direction == RecentObservationComparisonDirection.Low ? ranking.LowPercentile : ranking.HighPercentile;
        return $"{direction} than {FormatPercent(percentile)}% of comparable periods";
    }

    // <summary>
    // Record status considering both ends of the historical range: a value at the
    // top (highest ever) or bottom (lowest ever) of the range is a record. Values in
    // between return <see cref="RecentObservationRecordStatus.None"/> and should be
    // shown as a rank toward whichever end they are nearer.
    // </summary>
    public static RecentObservationRecordStatus DetermineRecordStatus(RecentObservationComparisonResult ranking)
    {
        if (ranking.IsNewHighRecord || ranking.IsNewLowRecord)
        {
            return RecentObservationRecordStatus.NewRecord;
        }

        var equalsHighest = ranking.IsTiedHighRecord && ranking.HighRank == 1;
        var equalsLowest = ranking.IsTiedLowRecord && ranking.LowRank == 1;

        return equalsHighest || equalsLowest
            ? RecentObservationRecordStatus.EqualRecord
            : RecentObservationRecordStatus.None;
    }

    public static string FormatOrdinal(int value)
    {
        var abs = Math.Abs(value);
        var lastTwoDigits = abs % 100;

        if (lastTwoDigits is >= 11 and <= 13)
        {
            return $"{value}th";
        }

        return (abs % 10) switch
        {
            1 => $"{value}st",
            2 => $"{value}nd",
            3 => $"{value}rd",
            _ => $"{value}th",
        };
    }

    public static int FormatPercent(double percentile)
    {
        return Math.Clamp((int)Math.Round(percentile, MidpointRounding.AwayFromZero), 0, 99);
    }

    private static RecentObservationComparisonDirection GetDirection(double highPercentile, double lowPercentile)
    {
        if (highPercentile >= 60d && highPercentile >= lowPercentile)
        {
            return RecentObservationComparisonDirection.High;
        }

        if (lowPercentile >= 60d)
        {
            return RecentObservationComparisonDirection.Low;
        }

        return RecentObservationComparisonDirection.Neutral;
    }
}
