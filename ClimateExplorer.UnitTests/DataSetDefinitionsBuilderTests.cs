namespace ClimateExplorer.UnitTests;

using System;
using System.Linq;
using ClimateExplorer.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DataSetDefinitionsBuilderTests
{
    private static readonly Guid BomId = Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E");
    private static readonly Guid EcadId = Guid.Parse("265289F3-D375-437C-A642-A5EC49C8B5F7");
    private static readonly Guid GhcndId = Guid.Parse("87C65C34-C689-4BA1-8061-626E4A63D401");
    private static readonly Guid GhcndpId = Guid.Parse("5BBEAF4C-B459-410E-9B77-470905CB1E46");

    [TestMethod]
    public void BuildDataSetDefinitions_BomAndGhcndDefinitions_BomPrecedesGhcndForTempMaxTempMinAndPrecipitation()
    {
        var dataSetDefinitions = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

        var bomIndex = dataSetDefinitions.FindIndex(x => x.Id == BomId);
        var ghcndIndex = dataSetDefinitions.FindIndex(x => x.Id == GhcndId);
        var ghcndpIndex = dataSetDefinitions.FindIndex(x => x.Id == GhcndpId);

        Assert.AreNotEqual(-1, bomIndex, "BOM data set definition not found.");
        Assert.AreNotEqual(-1, ghcndIndex, "GHCNd data set definition not found.");
        Assert.AreNotEqual(-1, ghcndpIndex, "GHCNdp data set definition not found.");

        Assert.IsLessThan(ghcndIndex, bomIndex, "BOM must precede GHCNd so TempMax/TempMin resolve to BOM whenever a location is mapped in both.");
        Assert.IsLessThan(ghcndpIndex, bomIndex, "BOM must precede GHCNdp so Precipitation resolves to BOM whenever a location is mapped in both.");
    }

    [TestMethod]
    public void BuildDataSetDefinitions_EcadAndGhcndDefinitions_EcadPrecedesGhcndForEveryMeasurementItPublishes()
    {
        var dataSetDefinitions = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

        var ecadIndex = dataSetDefinitions.FindIndex(x => x.Id == EcadId);
        var ghcndIndex = dataSetDefinitions.FindIndex(x => x.Id == GhcndId);
        var ghcndpIndex = dataSetDefinitions.FindIndex(x => x.Id == GhcndpId);

        Assert.AreNotEqual(-1, ecadIndex, "ECA&D data set definition not found.");
        Assert.IsLessThan(ghcndIndex, ecadIndex, "ECA&D must precede GHCNd so TempMax/TempMin resolve to ECA&D whenever a location is mapped in both.");
        Assert.IsLessThan(ghcndpIndex, ecadIndex, "ECA&D must precede GHCNdp so Precipitation resolves to ECA&D whenever a location is mapped in both.");
    }

    [TestMethod]
    public void BuildDataSetDefinitions_EcadMeasurements_CarryTheSameAdjustmentAsTheGhcndOnesTheyPreempt()
    {
        // Resolution matches on (DataType, DataAdjustment, DataResolution), so preceding GHCNd only wins a
        // location if the adjustment matches too - an ECA&D series tagged differently would sit alongside
        // GHCNd's rather than in front of it.
        var dataSetDefinitions = DataSetDefinitionsBuilder.BuildDataSetDefinitions();
        var ecad = dataSetDefinitions.Single(x => x.Id == EcadId).MeasurementDefinitions!;
        var ghcnd = dataSetDefinitions.Single(x => x.Id == GhcndId).MeasurementDefinitions!;
        var ghcndp = dataSetDefinitions.Single(x => x.Id == GhcndpId).MeasurementDefinitions!;

        foreach (var ghcndMeasurement in ghcnd.Concat(ghcndp))
        {
            var ecadMeasurement = ecad.SingleOrDefault(x =>
                x.DataType == ghcndMeasurement.DataType && x.DataResolution == ghcndMeasurement.DataResolution);

            Assert.IsNotNull(ecadMeasurement, $"ECA&D publishes no daily {ghcndMeasurement.DataType}.");
            Assert.AreEqual(
                ghcndMeasurement.DataAdjustment,
                ecadMeasurement.DataAdjustment,
                $"ECA&D's {ghcndMeasurement.DataType} must carry GHCNd's adjustment to preempt it.");
        }
    }
}
