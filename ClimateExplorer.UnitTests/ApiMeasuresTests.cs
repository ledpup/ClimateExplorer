namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Values calculated outside the client can't be checked by shifting their inputs, so each field's kind
/// is pinned here against what the field means.
/// </summary>
[TestClass]
public class ApiMeasuresTests
{
    [TestMethod]
    public void WarmingAnomaly_AnyLocation_IsChangeInCelsius()
    {
        var measure = ApiMeasures.WarmingAnomaly(CreateLocation(warmingAnomaly: 1.2))!.Value;

        Assert.AreEqual(MeasureKind.Change, measure.Kind);
        Assert.AreEqual(UnitOfMeasure.DegreesCelsius, measure.Unit);
        Assert.AreEqual("+2.2°F", UnitFormatter.UsCustomary.Format(measure, signed: true));
    }

    [TestMethod]
    public void WarmingAnomaly_NoAnomaly_IsNull()
    {
        Assert.IsNull(ApiMeasures.WarmingAnomaly(CreateLocation()));
    }

    [TestMethod]
    public void RecordHigh_AnyRecord_IsLevelInCelsius()
    {
        var measure = ApiMeasures.RecordHigh(CreateLocation(recordHigh: new DataRecord(2019, 1, 24, 46.6)))!.Value;

        Assert.AreEqual(MeasureKind.Level, measure.Kind);
        Assert.AreEqual(UnitOfMeasure.DegreesCelsius, measure.Unit);
        Assert.AreEqual("115.9°F", UnitFormatter.UsCustomary.Format(measure));
    }

    [TestMethod]
    public void Anomaly_CalculatedAnomaly_IsChange()
    {
        Assert.AreEqual(MeasureKind.Change, ApiMeasures.Anomaly(new CalculatedAnomaly { AnomalyValue = 0.8 }, UnitOfMeasure.DegreesCelsius).Kind);
    }

    [TestMethod]
    public void AnomalyPeriodAverage_AnyAverage_IsLevel()
    {
        Assert.AreEqual(MeasureKind.Level, ApiMeasures.AnomalyPeriodAverage(14.2, UnitOfMeasure.DegreesCelsius).Kind);
    }

    [TestMethod]
    public void HeatingScoreAnomalyBound_AnyBound_IsChangeInCelsius()
    {
        var measure = ApiMeasures.HeatingScoreAnomalyBound(0.5);

        Assert.AreEqual(MeasureKind.Change, measure.Kind);
        Assert.AreEqual(UnitOfMeasure.DegreesCelsius, measure.Unit);
    }

    [TestMethod]
    public void ClimateRecordFields_ValueAndAverage_AreLevels()
    {
        Assert.AreEqual(MeasureKind.Level, ApiMeasures.ClimateRecordValue(30.1, UnitOfMeasure.DegreesCelsius).Kind);
        Assert.AreEqual(MeasureKind.Level, ApiMeasures.ClimateRecordAverage(15.2, UnitOfMeasure.DegreesCelsius).Kind);
    }

    [TestMethod]
    public void ClimateRecordAnomaly_AnyAnomaly_IsChange()
    {
        Assert.AreEqual(MeasureKind.Change, ApiMeasures.ClimateRecordAnomaly(1.1, UnitOfMeasure.DegreesCelsius).Kind);
    }

    private static Location CreateLocation(double? warmingAnomaly = null, DataRecord? recordHigh = null)
    {
        return new Location
        {
            Id = System.Guid.NewGuid(),
            Name = "Testville",
            CountryCode = "TC",
            Coordinates = new Coordinates(1, 2),
            WarmingAnomaly = warmingAnomaly,
            RecordHigh = recordHigh,
        };
    }
}
