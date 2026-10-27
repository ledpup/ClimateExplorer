namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class SeriesDisplayUnitsTests
{
    [TestMethod]
    public void ResolveKind_PlainMean_IsLevel()
    {
        Assert.AreEqual(MeasureKind.Level, SeriesDisplayUnits.ResolveKind(CreateSeries(), UnitOfMeasure.DegreesCelsius));
    }

    [TestMethod]
    public void ResolveKind_AnomalyValue_IsChange()
    {
        Assert.AreEqual(MeasureKind.Change, SeriesDisplayUnits.ResolveKind(CreateSeries(value: SeriesValueOptions.Anomaly), UnitOfMeasure.DegreesCelsius));
    }

    [TestMethod]
    public void ResolveKind_CelsiusAnomalyUnit_IsChange()
    {
        Assert.AreEqual(MeasureKind.Change, SeriesDisplayUnits.ResolveKind(CreateSeries(), UnitOfMeasure.DegreesCelsiusAnomaly));
    }

    [TestMethod]
    [DataRow(SeriesDerivationTypes.DifferenceBetweenTwoSeries)]
    [DataRow(SeriesDerivationTypes.AverageOfAnomaliesInRegion)]
    public void ResolveKind_DifferenceDerivation_IsChange(SeriesDerivationTypes derivation)
    {
        Assert.AreEqual(MeasureKind.Change, SeriesDisplayUnits.ResolveKind(CreateSeries(derivation: derivation), UnitOfMeasure.DegreesCelsius));
    }

    [TestMethod]
    public void ResolveKind_AverageOfMultipleSeries_IsLevel()
    {
        Assert.AreEqual(MeasureKind.Level, SeriesDisplayUnits.ResolveKind(CreateSeries(derivation: SeriesDerivationTypes.AverageOfMultipleSeries), UnitOfMeasure.DegreesCelsius));
    }

    [TestMethod]
    public void ResolveKind_AnnualChange_IsChange()
    {
        Assert.AreEqual(MeasureKind.Change, SeriesDisplayUnits.ResolveKind(CreateSeries(temporalCalculation: TemporalCalculationOptions.AnnualChange), UnitOfMeasure.DegreesCelsius));
    }

    [TestMethod]
    [DataRow(SeriesTransformations.Negate)]
    [DataRow(SeriesTransformations.IsPositive)]
    [DataRow(SeriesTransformations.IsNegative)]
    [DataRow(SeriesTransformations.DayOfYearIfFrost)]
    [DataRow(SeriesTransformations.Custom)]
    public void Resolve_TransformedSeriesUsCustomary_StaysMetric(SeriesTransformations transformation)
    {
        var units = SeriesDisplayUnits.Resolve(CreateSeries(transformation: transformation), UnitOfMeasure.DegreesCelsius, UnitFormatter.UsCustomary);

        Assert.AreSame(UnitFormatter.Metric, units.Formatter);
        Assert.AreEqual(10, units.ToDisplay(10d));
    }

    [TestMethod]
    public void Resolve_SumOfCelsiusUsCustomary_StaysMetric()
    {
        var units = SeriesDisplayUnits.Resolve(CreateSeries(aggregation: SeriesAggregationOptions.Sum), UnitOfMeasure.DegreesCelsius, UnitFormatter.UsCustomary);

        Assert.AreSame(UnitFormatter.Metric, units.Formatter);
        Assert.AreEqual("°C", units.ShortLabel);
    }

    [TestMethod]
    public void Resolve_CumulativeCelsiusUsCustomary_StaysMetric()
    {
        var units = SeriesDisplayUnits.Resolve(CreateSeries(temporalCalculation: TemporalCalculationOptions.Cumulative), UnitOfMeasure.DegreesCelsius, UnitFormatter.UsCustomary);

        Assert.AreSame(UnitFormatter.Metric, units.Formatter);
    }

    [TestMethod]
    public void Resolve_SumOfCelsiusAnomaliesUsCustomary_Converts()
    {
        // A sum of differences is a difference, so no offset is involved.
        var units = SeriesDisplayUnits.Resolve(CreateSeries(aggregation: SeriesAggregationOptions.Sum, value: SeriesValueOptions.Anomaly), UnitOfMeasure.DegreesCelsius, UnitFormatter.UsCustomary);

        Assert.AreSame(UnitFormatter.UsCustomary, units.Formatter);
        Assert.AreEqual(1.8, units.ToDisplay(1d), 1e-9);
    }

    [TestMethod]
    public void Resolve_SumOfMillimetresUsCustomary_Converts()
    {
        var units = SeriesDisplayUnits.Resolve(CreateSeries(aggregation: SeriesAggregationOptions.Sum), UnitOfMeasure.Millimetres, UnitFormatter.UsCustomary);

        Assert.AreEqual("in", units.ShortLabel);
        Assert.AreEqual(1, units.ToDisplay(25.4), 1e-9);
    }

    [TestMethod]
    public void Resolve_PlainMeanUsCustomary_ConvertsWithOffset()
    {
        var units = SeriesDisplayUnits.Resolve(CreateSeries(), UnitOfMeasure.DegreesCelsius, UnitFormatter.UsCustomary);

        Assert.AreEqual("°F", units.ShortLabel);
        Assert.AreEqual(50, units.ToDisplay(10d), 1e-9);
        Assert.IsNull(units.ToDisplay((double?)null));
    }

    [TestMethod]
    public void Resolve_AxisIds_MetricSeriesInUsCustomaryGetsItsOwnAxis()
    {
        var converted = SeriesDisplayUnits.Resolve(CreateSeries(), UnitOfMeasure.DegreesCelsius, UnitFormatter.UsCustomary);
        var leftInMetric = SeriesDisplayUnits.Resolve(CreateSeries(aggregation: SeriesAggregationOptions.Sum), UnitOfMeasure.DegreesCelsius, UnitFormatter.UsCustomary);
        var metric = SeriesDisplayUnits.Resolve(CreateSeries(), UnitOfMeasure.DegreesCelsius, UnitFormatter.Metric);

        // The usual id is unchanged in both systems, so scale-to-zero settings keyed by it still apply.
        Assert.AreEqual("degreesCelsius", converted.AxisId);
        Assert.AreEqual("degreesCelsius", metric.AxisId);
        Assert.AreNotEqual(converted.AxisId, leftInMetric.AxisId);
    }

    internal static ChartSeriesDefinition CreateSeries(
        SeriesTransformations transformation = SeriesTransformations.Identity,
        SeriesValueOptions value = SeriesValueOptions.Value,
        SeriesDerivationTypes derivation = SeriesDerivationTypes.ReturnSingleSeries,
        TemporalCalculationOptions temporalCalculation = TemporalCalculationOptions.None,
        SeriesAggregationOptions aggregation = SeriesAggregationOptions.Mean)
    {
        return new ChartSeriesDefinition
        {
            SeriesDerivationType = derivation,
            SeriesTransformation = transformation,
            CustomTransformation = transformation == SeriesTransformations.Custom ? "x >= 35" : null,
            Value = value,
            TemporalCalculation = temporalCalculation,
            Aggregation = aggregation,
            BinGranularity = BinGranularities.ByYear,
            DisplayStyle = SeriesDisplayStyle.Line,
        };
    }
}
