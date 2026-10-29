namespace ClimateExplorer.Web.UnitTests;

using System.Collections.Generic;
using ClimateExplorer.Web.Client.Units;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Formats like the formatter it wraps, and records every measure it's given, in order. Used by the
/// offset-shift tests to check each measure's kind against what the calculation actually did.
/// </summary>
internal sealed class RecordingUnitFormatter(IUnitFormatter inner) : IUnitFormatter
{
    public List<Measure> Measures { get; } = [];

    public UnitSystem System => inner.System;

    public bool Converts(UnitOfMeasure unit) => inner.Converts(unit);

    public string Format(Measure measure, bool signed = false, int extraDecimals = 0, int minimumDecimals = 0)
    {
        Measures.Add(measure);
        return inner.Format(measure, signed, extraDecimals, minimumDecimals);
    }

    public string FormatNumber(Measure measure, bool signed = false, int extraDecimals = 0, int minimumDecimals = 0)
    {
        Measures.Add(measure);
        return inner.FormatNumber(measure, signed, extraDecimals, minimumDecimals);
    }

    public double ToDisplay(Measure measure)
    {
        Measures.Add(measure);
        return inner.ToDisplay(measure);
    }

    public string ShortLabel(UnitOfMeasure unit) => inner.ShortLabel(unit);

    public string Label(UnitOfMeasure unit) => inner.Label(unit);

    public int Decimals(UnitOfMeasure unit) => inner.Decimals(unit);

    public bool IsTight(UnitOfMeasure unit) => inner.IsTight(unit);
}

/// <summary>
/// The offset-shift check. A calculation is run on input temperatures X and on X + shift, and every
/// measure it passed to the formatter is recorded. Paired in order, each °C level must have moved by
/// exactly the shift, and each °C change - an anomaly, slope or spread - must not have moved at all. A
/// measure tagged with the wrong kind fails this whatever anyone believed it to be.
/// </summary>
internal static class OffsetShift
{
    private const double Tolerance = 1e-6;

    public static void AssertLevelsMoveAndChangesDoNot(IReadOnlyList<Measure> original, IReadOnlyList<Measure> shifted, double shift, int minimumLevels = 1, int minimumChanges = 1)
    {
        Assert.HasCount(original.Count, shifted, "Both runs must format the same measures in the same order.");

        var levels = 0;
        var changes = 0;

        for (var i = 0; i < original.Count; i++)
        {
            var a = original[i];
            var b = shifted[i];

            Assert.AreEqual(a.Unit, b.Unit, $"Measure {i} changed unit.");
            Assert.AreEqual(a.Kind, b.Kind, $"Measure {i} changed kind.");

            if (a.Unit != UnitOfMeasure.DegreesCelsius)
            {
                continue;
            }

            var moved = b.Value - a.Value;

            if (a.Kind == MeasureKind.Level)
            {
                levels++;
                Assert.AreEqual(shift, moved, Tolerance, $"Measure {i} is tagged Level ({a.Value}) but moved by {moved}, not {shift}: it's a difference.");
            }
            else
            {
                changes++;
                Assert.AreEqual(0, moved, Tolerance, $"Measure {i} is tagged Change ({a.Value}) but moved by {moved}: it's a level.");
            }
        }

        // Guard against a vacuous pass, e.g. a test that silently stopped formatting anything.
        Assert.IsGreaterThanOrEqualTo(minimumLevels, levels, "Too few °C levels were checked.");
        Assert.IsGreaterThanOrEqualTo(minimumChanges, changes, "Too few °C changes were checked.");
    }
}
