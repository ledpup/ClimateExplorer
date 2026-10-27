namespace ClimateExplorer.Web.Client.Units;

using static ClimateExplorer.Core.Enums;

/// <summary>
/// A finished, metric number on its way to the screen. There is no public constructor, so every call
/// site states what the number means with <see cref="Level"/> or <see cref="Change"/>.
/// </summary>
/// <remarks>
/// Only <see cref="IUnitFormatter"/> turns a measure into display units. Nothing converted is ever fed
/// back into a calculation.
/// </remarks>
public readonly record struct Measure
{
    private Measure(double value, UnitOfMeasure unit, MeasureKind kind)
    {
        Value = value;
        Unit = unit;
        Kind = kind;
    }

    /// <summary>The metric value, exactly as it was calculated.</summary>
    public double Value { get; }

    /// <summary>The metric unit <see cref="Value"/> is in.</summary>
    public UnitOfMeasure Unit { get; }

    public MeasureKind Kind { get; }

    /// <summary>A temperature, total or extent, rather than a difference between two of them.</summary>
    public static Measure Level(double value, UnitOfMeasure unit) => new(value, unit, MeasureKind.Level);

    /// <summary>A difference between two levels: an anomaly, slope, spread or "above average" amount.</summary>
    public static Measure Change(double value, UnitOfMeasure unit) => new(value, unit, MeasureKind.Change);
}
