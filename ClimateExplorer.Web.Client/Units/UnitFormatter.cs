namespace ClimateExplorer.Web.Client.Units;

using System.Globalization;
using ClimateExplorer.Core.DataPreparation;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Formats metric measures for one unit system. There are exactly two instances, so swapping one for
/// the other is what tells cascading-parameter consumers that the system changed.
/// </summary>
/// <remarks>
/// The metric instance does no arithmetic at all, and takes its labels and rounding from the existing
/// <see cref="ClimateExplorer.Core.Enums"/> helpers, so metric output is exactly what the site showed
/// before this layer existed.
/// </remarks>
public sealed class UnitFormatter : IUnitFormatter
{
    // All exact by definition: 1 in = 25.4 mm, and 1 mi = 1.609344 km so 1 mi² = 2.589988110336 km². 1 lb =
    // 0.45359237 kg, 1 ft = 0.3048 m so 1 ft³ = 0.028316846592 m³, and 1 US gal = 231 in³ = 3.785411784 L. A psi is
    // a pound-force (1 lb under standard gravity, 9.80665 m/s²) on a square inch: 6.894757293168361 kPa.
    private const double MillimetresPerInch = 25.4;
    private const double SqKmPerSqMile = 2.589988110336;
    private const double KilogramsPerPound = 0.45359237;
    private const double CubicMetresPerCubicFoot = 0.028316846592;
    private const double LitresPerUsGallon = 3.785411784;
    private const double KilopascalsPerPsi = KilogramsPerPound * 9.80665 / (MillimetresPerInch * MillimetresPerInch) * 1000;
    private const double SecondsPerHour = 3600;

    // The allow-list. A unit that isn't here shows unchanged in both systems, including any unit added
    // to Core in future. Only DegreesCelsius has an offset, and it applies to levels only: a difference
    // between two temperatures is scaled but never offset. DegreesCelsiusAnomaly is always a
    // difference, so it has no offset whatever kind a caller states.
    private static readonly Dictionary<UnitOfMeasure, Conversion> UsCustomaryConversions = new()
    {
        [UnitOfMeasure.DegreesCelsius] = new(1.8, 32, "°F", "Degrees Fahrenheit (°F)", 1, Tight: true),
        [UnitOfMeasure.DegreesCelsiusAnomaly] = new(1.8, 0, "°F anomaly", "Temperature anomaly (°F)", 1, Tight: false),
        [UnitOfMeasure.Millimetres] = new(1 / MillimetresPerInch, 0, "in", "Inches (in)", 2, Tight: false),
        [UnitOfMeasure.SqKm] = new(1 / SqKmPerSqMile, 0, "mi²", "Square miles (mi²)", 0, Tight: false),
        [UnitOfMeasure.MillionSqKm] = new(1 / SqKmPerSqMile, 0, "million mi²", "Million square miles", 2, Tight: false),
        [UnitOfMeasure.Kilograms] = new(1 / KilogramsPerPound, 0, "lb", "Pounds (lb)", 1, Tight: false),
        [UnitOfMeasure.CubicMetres] = new(1 / CubicMetresPerCubicFoot, 0, "ft³", "Cubic feet (ft³)", 0, Tight: false),
        [UnitOfMeasure.Kilopascals] = new(1 / KilopascalsPerPsi, 0, "psi", "Pounds per square inch (psi)", 2, Tight: false),

        // US gas appliances and meters are rated in cubic feet per hour, and liquid-fuel burners in gallons per hour.
        [UnitOfMeasure.GasLitresPerSecond] = new(SecondsPerHour / (CubicMetresPerCubicFoot * 1000), 0, "ft³/h", "Cubic feet per hour (ft³/h)", 1, Tight: false),
        [UnitOfMeasure.LiquidLitresPerSecond] = new(SecondsPerHour / LitresPerUsGallon, 0, "gal/h", "US gallons per hour (gal/h)", 1, Tight: false),
    };

    // The metric units the site has always written without a space before the label ("14.0°C", "12mm").
    private static readonly HashSet<UnitOfMeasure> TightMetricUnits =
    [
        UnitOfMeasure.DegreesCelsius,
        UnitOfMeasure.Millimetres,
    ];

    private UnitFormatter(UnitSystem system)
    {
        System = system;
    }

    public static UnitFormatter Metric { get; } = new(UnitSystem.Metric);

    public static UnitFormatter UsCustomary { get; } = new(UnitSystem.UsCustomary);

    public UnitSystem System { get; }

    public static UnitFormatter For(UnitSystem system)
    {
        return system switch
        {
            UnitSystem.Metric => Metric,
            UnitSystem.UsCustomary => UsCustomary,
            _ => throw new ArgumentOutOfRangeException(nameof(system), system, null),
        };
    }

    public bool Converts(UnitOfMeasure unit)
    {
        return TryGetConversion(unit, out _);
    }

    public string Format(Measure measure, bool signed = false, int extraDecimals = 0, int minimumDecimals = 0)
    {
        var number = FormatNumber(measure, signed, extraDecimals, minimumDecimals);
        var label = ShortLabel(measure.Unit);

        return IsTight(measure.Unit) ? $"{number}{label}" : $"{number} {label}";
    }

    public string FormatNumber(Measure measure, bool signed = false, int extraDecimals = 0, int minimumDecimals = 0)
    {
        var value = ToDisplay(measure);
        var decimals = Math.Max(Decimals(measure.Unit) + extraDecimals, minimumDecimals);
        var text = value.ToString(NumberFormat(decimals), CultureInfo.InvariantCulture);

        return signed && value >= 0 ? $"+{text}" : text;
    }

    public double ToDisplay(Measure measure)
    {
        if (!Enum.IsDefined(measure.Kind))
        {
            throw new ArgumentException("A measure must be created with Measure.Level or Measure.Change.", nameof(measure));
        }

        if (!TryGetConversion(measure.Unit, out var conversion))
        {
            return measure.Value;
        }

        var offset = measure.Kind == MeasureKind.Level ? conversion.LevelOffset : 0;

        return (measure.Value * conversion.Scale) + offset;
    }

    public double FromDisplay(double value, UnitOfMeasure unit)
    {
        if (!TryGetConversion(unit, out var conversion))
        {
            return value;
        }

        return (value - conversion.LevelOffset) / conversion.Scale;
    }

    public string ShortLabel(UnitOfMeasure unit)
    {
        return TryGetConversion(unit, out var conversion)
            ? conversion.ShortLabel
            : UnitOfMeasureLabelShort(unit);
    }

    public string Label(UnitOfMeasure unit)
    {
        // The public overload with an untransformed, plain-value series returns Core's own long label.
        return TryGetConversion(unit, out var conversion)
            ? conversion.Label
            : UnitOfMeasureLabel(SeriesTransformations.Identity, null, unit, SeriesAggregationOptions.Mean, SeriesValueOptions.Value);
    }

    public int Decimals(UnitOfMeasure unit)
    {
        return TryGetConversion(unit, out var conversion)
            ? conversion.Decimals
            : UnitOfMeasureRounding(unit);
    }

    /// <summary>Whether the label follows the number with no space, as in "14.0°C".</summary>
    public bool IsTight(UnitOfMeasure unit)
    {
        return TryGetConversion(unit, out var conversion)
            ? conversion.Tight
            : TightMetricUnits.Contains(unit);
    }

    private static string NumberFormat(int decimals)
    {
        return decimals <= 0 ? "0" : "0." + new string('0', decimals);
    }

    private bool TryGetConversion(UnitOfMeasure unit, out Conversion conversion)
    {
        if (System == UnitSystem.UsCustomary && UsCustomaryConversions.TryGetValue(unit, out var found))
        {
            conversion = found;
            return true;
        }

        conversion = null!;
        return false;
    }

    private sealed record Conversion(double Scale, double LevelOffset, string ShortLabel, string Label, int Decimals, bool Tight);
}
