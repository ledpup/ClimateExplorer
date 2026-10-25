namespace ClimateExplorer.Web.Client.Units;

using static ClimateExplorer.Core.Enums;

/// <summary>
/// The only code that converts a number out of metric or writes a unit label. Components receive the
/// current one as a cascading parameter from <c>Routes</c>.
/// </summary>
public interface IUnitFormatter
{
    UnitSystem System { get; }

    /// <summary>Whether values in <paramref name="unit"/> are shown in a different unit in this system.</summary>
    bool Converts(UnitOfMeasure unit);

    /// <summary>
    /// The value and its unit, e.g. "68.2°F". Rounds once, after converting, to <see cref="Decimals"/>
    /// plus <paramref name="extraDecimals"/> places, and to at least <paramref name="minimumDecimals"/>.
    /// <paramref name="signed"/> puts a "+" in front of values that aren't negative.
    /// </summary>
    string Format(Measure measure, bool signed = false, int extraDecimals = 0, int minimumDecimals = 0);

    /// <summary>The rounded value without its unit, for places that show the unit separately.</summary>
    string FormatNumber(Measure measure, bool signed = false, int extraDecimals = 0, int minimumDecimals = 0);

    /// <summary>The unrounded value in display units, for Chart.js data and CSV cells.</summary>
    double ToDisplay(Measure measure);

    /// <summary>"°F", "in", or the unchanged metric label.</summary>
    string ShortLabel(UnitOfMeasure unit);

    /// <summary>"Degrees Fahrenheit (°F)", or the unchanged metric label.</summary>
    string Label(UnitOfMeasure unit);

    /// <summary>Decimal places for values in <paramref name="unit"/>, once converted.</summary>
    int Decimals(UnitOfMeasure unit);

    /// <summary>Whether the label follows the number with no space, as in "14.0°C".</summary>
    bool IsTight(UnitOfMeasure unit);
}
