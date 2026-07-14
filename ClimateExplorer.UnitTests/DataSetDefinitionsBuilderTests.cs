namespace ClimateExplorer.UnitTests;

using System;
using ClimateExplorer.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DataSetDefinitionsBuilderTests
{
    private static readonly Guid BomId = Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E");
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
}
