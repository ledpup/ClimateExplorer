namespace ClimateExplorer.Web.UnitTests;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class AnomalyTooltipTests
{
    [TestMethod]
    public void Build_NoAnomaly_DescriptionWithoutAverages()
    {
        var tooltip = AnomalyTooltip.Build("Warmer.", null, UnitOfMeasure.DegreesCelsius, UnitFormatter.Metric);

        Assert.AreEqual("<p>Warmer.</p><p>Click for more information.</p>", tooltip);
    }

    [TestMethod]
    public void Build_Temperature_ListsFirstHalfThenLast30YearsAverages()
    {
        var tooltip = AnomalyTooltip.Build("Warmer.", CreateAnomaly(21.34, 22.46), UnitOfMeasure.DegreesCelsius, UnitFormatter.Metric);

        Assert.AreEqual(
            "<p>Warmer.</p><ul><li>1910–1966 average: 21.34°C</li><li>1995–2024 average: 22.46°C</li></ul><p>Click for more information.</p>",
            tooltip);
    }

    [TestMethod]
    public void Build_UsCustomary_ConvertsAverages()
    {
        var tooltip = AnomalyTooltip.Build("Warmer.", CreateAnomaly(20, 21), UnitOfMeasure.DegreesCelsius, UnitFormatter.UsCustomary);

        StringAssert.Contains(tooltip, "°F");
        Assert.DoesNotContain("°C", tooltip);
    }

    [TestMethod]
    public void Build_AnyAnomaly_FormatsAveragesAsLevels()
    {
        var units = new RecordingUnitFormatter(UnitFormatter.Metric);

        AnomalyTooltip.Build("Wetter.", CreateAnomaly(612.3, 640.8), UnitOfMeasure.Millimetres, units);

        Assert.HasCount(2, units.Measures);
        Assert.IsTrue(units.Measures.TrueForAll(x => x.Kind == MeasureKind.Level && x.Unit == UnitOfMeasure.Millimetres));
    }

    private static CalculatedAnomaly CreateAnomaly(double firstHalf, double last30) => new()
    {
        AverageOfFirstHalf = firstHalf,
        AverageOfLast30Years = last30,
        AnomalyValue = last30 - firstHalf,
        FirstYearInFirstHalf = 1910,
        LastYearInFirstHalf = 1966,
        FirstYearInLast30Years = 1995,
        LastYearInLast30Years = 2024,
    };
}
