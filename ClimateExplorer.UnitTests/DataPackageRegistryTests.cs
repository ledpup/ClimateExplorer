namespace ClimateExplorer.UnitTests;

using System;
using System.IO;
using ClimateExplorer.DataPipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DataPackageRegistryTests
{
    [TestMethod]
    public void RegisterLogicalFile_SourceRegisteredTwice_ThrowsInvalidDataException()
    {
        var registry = new DataPackageRegistry();
        var sourcePath = Path.Combine(Path.GetTempPath(), "source.csv");
        registry.RegisterLogicalFile(sourcePath, "Dataset/source.csv");

        Assert.ThrowsExactly<InvalidDataException>(
            () => registry.RegisterLogicalFile(sourcePath, "OtherDataset/source.csv"));
    }

    [TestMethod]
    public void RegisterLogicalFile_LogicalPathRegisteredTwice_ThrowsInvalidDataException()
    {
        var registry = new DataPackageRegistry();
        registry.RegisterLogicalFile(Path.Combine(Path.GetTempPath(), "source-1.csv"), "Dataset/source.csv");

        Assert.ThrowsExactly<InvalidDataException>(
            () => registry.RegisterLogicalFile(Path.Combine(Path.GetTempPath(), "source-2.csv"), "Dataset/source.csv"));
    }

    [TestMethod]
    public void RegisterPhysicalAsset_AssetRegisteredTwice_ThrowsInvalidDataException()
    {
        var registry = new DataPackageRegistry();
        registry.RegisterPhysicalAsset("Dataset/source.zip");

        Assert.ThrowsExactly<InvalidDataException>(
            () => registry.RegisterPhysicalAsset("Dataset/source.zip"));
    }

    [TestMethod]
    public void RegisterArchiveSource_MultipleEntries_RegistersSourceOnceAndEveryLogicalEntry()
    {
        var registry = new DataPackageRegistry();
        var sourcePath = Path.Combine(Path.GetTempPath(), "source.zip");

        registry.RegisterArchiveSource(sourcePath, ["Dataset/source.zip!/one.csv", "Dataset/source.zip!/two.csv"]);

        Assert.HasCount(1, registry.SourcePaths);
        Assert.AreEqual(2, registry.LogicalFileCount);
    }
}
