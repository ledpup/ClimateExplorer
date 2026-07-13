namespace ClimateExplorer.UnitTests;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.WebApi.DataRetrieval;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DataSetSourceInfrastructureTests
{
    private string temporaryRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerSourceInfrastructureTests-{Guid.NewGuid():N}");
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
    public async Task PublishAsync_ValidatedCandidate_ReplacesExistingSourceAndRemovesSiblingTemporaryFile()
    {
        var datasetsRoot = Path.Combine(temporaryRoot, "Datasets");
        var candidatePath = Path.Combine(temporaryRoot, "candidate.txt");
        var destinationPath = Path.Combine(datasetsRoot, "CO2", "co2.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await File.WriteAllTextAsync(candidatePath, "new-source");
        await File.WriteAllTextAsync(destinationPath, "old-source");
        var store = new DataSetSourceFileStore(datasetsRoot);

        await store.PublishAsync(candidatePath, @"CO2\co2.txt", CancellationToken.None);

        Assert.AreEqual("new-source", await File.ReadAllTextAsync(destinationPath));
        Assert.IsEmpty(Directory.GetFiles(Path.GetDirectoryName(destinationPath)!, "*.tmp-*"));
    }

    [TestMethod]
    public async Task GetFileInfoAsync_PublishedSource_ReturnsLengthAndSha256()
    {
        var datasetsRoot = Path.Combine(temporaryRoot, "Datasets");
        var sourcePath = Path.Combine(datasetsRoot, "CO2", "co2.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        await File.WriteAllTextAsync(sourcePath, "source");
        var store = new DataSetSourceFileStore(datasetsRoot);

        var fileInfo = await store.GetFileInfoAsync(@"CO2\co2.txt", CancellationToken.None);

        Assert.IsNotNull(fileInfo);
        Assert.AreEqual(new FileInfo(sourcePath).Length, fileInfo.Length);
        Assert.AreEqual(64, fileInfo.Sha256.Length);
    }

    [TestMethod]
    public async Task PublishAsync_PathOutsideDatasetsFolder_ThrowsAndPreservesExistingSource()
    {
        var datasetsRoot = Path.Combine(temporaryRoot, "Datasets");
        var candidatePath = Path.Combine(temporaryRoot, "candidate.txt");
        var existingPath = Path.Combine(datasetsRoot, "source.txt");
        Directory.CreateDirectory(datasetsRoot);
        await File.WriteAllTextAsync(candidatePath, "candidate");
        await File.WriteAllTextAsync(existingPath, "existing");
        var store = new DataSetSourceFileStore(datasetsRoot);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => store.PublishAsync(candidatePath, @"..\outside.txt", CancellationToken.None));

        Assert.AreEqual("existing", await File.ReadAllTextAsync(existingPath));
    }

    [TestMethod]
    public void Dispose_WorkspaceWithFiles_DeletesEntireTemporaryDirectory()
    {
        var workspace = new DataSetDownloadWorkspaceFactory().Create();
        var workspacePath = workspace.Path;
        File.WriteAllText(Path.Combine(workspacePath, "download.txt"), "content");

        workspace.Dispose();

        Assert.IsFalse(Directory.Exists(workspacePath));
    }

    [TestMethod]
    public async Task AcquireAsync_SameAsset_BlocksUntilFirstLeaseIsReleased()
    {
        var locks = new DataSetAssetLockProvider();
        var firstLease = await locks.AcquireAsync("asset", CancellationToken.None);
        var secondLeaseTask = locks.AcquireAsync("asset", CancellationToken.None).AsTask();

        Assert.IsFalse(secondLeaseTask.IsCompleted);

        await firstLease.DisposeAsync();
        var secondLease = await secondLeaseTask;
        await secondLease.DisposeAsync();
    }

    [TestMethod]
    public async Task AcquireAsync_DifferentAssets_AllowsConcurrentLeases()
    {
        var locks = new DataSetAssetLockProvider();
        await using var firstLease = await locks.AcquireAsync("asset-1", CancellationToken.None);

        var secondLeaseTask = locks.AcquireAsync("asset-2", CancellationToken.None).AsTask();

        Assert.IsTrue(secondLeaseTask.IsCompletedSuccessfully);
        await (await secondLeaseTask).DisposeAsync();
    }
}
