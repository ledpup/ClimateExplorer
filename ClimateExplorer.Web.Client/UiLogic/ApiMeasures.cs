namespace ClimateExplorer.Web.UiLogic;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The kind of every number that arrives already calculated - from the API's location model and climate
/// records, or from a Core calculator - declared once, here, rather than at each component that shows it.
/// </summary>
/// <remarks>
/// These values can't be checked by shifting their inputs in a client test, because they weren't
/// calculated in the client. So each one is declared here, against what the field means, and has a test
/// of its own. A component that shows one of these values must take its <see cref="Measure"/> from here.
/// </remarks>
public static class ApiMeasures
{
    /// <summary>
    /// <see cref="Location.WarmingAnomaly"/>: how much warmer the recent mean temperature is than the early
    /// one. A difference.
    /// </summary>
    public static Measure? WarmingAnomaly(Location location)
    {
        return location.WarmingAnomaly is { } anomaly ? Measure.Change(anomaly, UnitOfMeasure.DegreesCelsius) : null;
    }

    /// <summary><see cref="Location.RecordHigh"/>: the highest temperature recorded. A level.</summary>
    public static Measure? RecordHigh(Location location)
    {
        return location.RecordHigh?.Value is { } value ? Measure.Level(value, UnitOfMeasure.DegreesCelsius) : null;
    }

    /// <summary>
    /// <see cref="CalculatedAnomaly.AnomalyValue"/>: the last-30-years average minus the first-half average.
    /// A difference.
    /// </summary>
    public static Measure Anomaly(CalculatedAnomaly anomaly, UnitOfMeasure unit)
    {
        return Measure.Change(anomaly.AnomalyValue, unit);
    }

    /// <summary>
    /// One of a <see cref="CalculatedAnomaly"/>'s period averages (first half, last 30 years, full period). A
    /// level.
    /// </summary>
    public static Measure AnomalyPeriodAverage(double average, UnitOfMeasure unit)
    {
        return Measure.Level(average, unit);
    }

    /// <summary>
    /// A bound of a heating score's warming anomaly range (<see cref="HeatingScoreRow"/>). A difference, like the
    /// anomalies it bounds.
    /// </summary>
    public static Measure HeatingScoreAnomalyBound(double bound)
    {
        return Measure.Change(bound, UnitOfMeasure.DegreesCelsius);
    }

    /// <summary>A climate record's value: the observation, or month or year average, it ranks. A level.</summary>
    public static Measure ClimateRecordValue(double value, UnitOfMeasure unit)
    {
        return Measure.Level(value, unit);
    }

    /// <summary>A climate record year's average. A level.</summary>
    public static Measure ClimateRecordAverage(double average, UnitOfMeasure unit)
    {
        return Measure.Level(average, unit);
    }

    /// <summary>A climate record year's anomaly against the record's mean. A difference.</summary>
    public static Measure ClimateRecordAnomaly(double anomaly, UnitOfMeasure unit)
    {
        return Measure.Change(anomaly, unit);
    }
}
