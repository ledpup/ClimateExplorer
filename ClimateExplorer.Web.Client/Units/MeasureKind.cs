namespace ClimateExplorer.Web.Client.Units;

/// <summary>
/// What a number means, which decides whether the °C → °F offset applies. There is deliberately no
/// zero member, so a <c>default(Measure)</c> is rejected by the formatter instead of being treated as
/// either kind.
/// </summary>
public enum MeasureKind
{
    /// <summary>
    /// A temperature, rainfall total or extent: a reading, mean, median, min, max, record or period
    /// average of readings.
    /// </summary>
    Level = 1,

    /// <summary>
    /// A difference between levels: an anomaly, trend slope, standard deviation, range or
    /// "above/below average" amount.
    /// </summary>
    Change = 2,
}
