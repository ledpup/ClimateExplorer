namespace ClimateExplorer.UnitTests;

using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApi.AcornSat;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class AcornSatDatasetIdsTests
{
    [TestMethod]
    public async Task AcornSat_WellKnownId_ResolvesToAcornSatDataSetDefinition()
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions();

        var dsd = definitions.Single(x => x.Id == AcornSatDatasetIds.AcornSat);

        Assert.AreEqual("ACORN-SAT", dsd.ShortName);
        Assert.IsTrue(dsd.MeasurementDefinitions!.All(x => x.DataAdjustment == DataAdjustment.Adjusted));
        Assert.IsNull(dsd.DataDownloaderKey, "ACORN-SAT must remain a manual/annual source with no automatic downloader.");
    }

    [TestMethod]
    public async Task Cdo_WellKnownId_ResolvesToCdoDataSetDefinition()
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions();

        var dsd = definitions.Single(x => x.Id == AcornSatDatasetIds.Cdo);

        Assert.AreEqual("CDO", dsd.ShortName);
        Assert.IsTrue(dsd.MeasurementDefinitions!.Any(x => x.DataAdjustment == DataAdjustment.Unadjusted && x.DataType == DataType.TempMax));
        Assert.AreEqual("bom-station", dsd.DataDownloaderKey);
    }
}
