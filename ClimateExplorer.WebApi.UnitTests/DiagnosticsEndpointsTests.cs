namespace ClimateExplorer.WebApi.UnitTests;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.WebApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DiagnosticsEndpointsTests
{
    // Adelaide's open BOM station is 023000 (see AcornSatClimateRecordServiceTests).
    private static readonly Guid AdelaideLocationId = Guid.Parse("70a07bb0-2220-402b-be83-f2c35edfdd12");
    private const string AdelaideAssetKey = "BOM/023000.ZIP";
    private string temporaryRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerDiagnosticsTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(temporaryRoot))
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    [TestMethod]
    public async Task GetSourceState_NoPublishedFileOrState_ReportsAssetWithNeither()
    {
        var result = await GetSourceState(new FileDataSetSourceStateStore(Path.Combine(temporaryRoot, "state")));

        var asset = result.Assets.Single(x => x.AssetKey == AdelaideAssetKey);
        Assert.IsNull(asset.File);
        Assert.IsNull(asset.State);
        Assert.IsFalse(asset.StateIsFresh);
        Assert.IsFalse(asset.FileMatchesState);
    }

    [TestMethod]
    public async Task GetSourceState_StateRecordsPublishedFile_ReportsFileMatchesState()
    {
        var filePath = Path.Combine(temporaryRoot, "datasets", "BOM", "023000.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "published");
        var fileInfo = (await new DataSetSourceFileStore(Path.Combine(temporaryRoot, "datasets")).GetFileInfoAsync(@"BOM\023000.zip", CancellationToken.None))!;
        var stateStore = new FileDataSetSourceStateStore(Path.Combine(temporaryRoot, "state"));
        await stateStore.PutAsync(
            new DataSetSourceState
            {
                AssetKey = AdelaideAssetKey,
                RelativePath = @"BOM\023000.zip",
                Length = fileInfo.Length,
                Sha256 = fileInfo.Sha256,
                RetrievedDate = DateTimeOffset.UtcNow,
            },
            CancellationToken.None);

        var result = await GetSourceState(stateStore);

        var asset = result.Assets.Single(x => x.AssetKey == AdelaideAssetKey);
        Assert.AreEqual(fileInfo.Sha256, asset.File!.Sha256);
        Assert.AreEqual(stateStore.GetStatePath(AdelaideAssetKey), asset.StateFilePath);
        Assert.IsTrue(asset.FileMatchesState);
    }

    private Task<DiagnosticsEndpoints.SourceStateDiagnostics> GetSourceState(IDataSetSourceStateStore stateStore)
    {
        return DiagnosticsEndpoints.GetSourceState(
            AdelaideLocationId,
            new DataSetSourceAssetResolver(Path.Combine(Folders.MetaDataFolder, "DataFileMapping")),
            new DataSetSourceFileStore(Path.Combine(temporaryRoot, "datasets")),
            stateStore,
            new DataSetFreshnessPolicy(TimeProvider.System),
            CancellationToken.None);
    }
}
