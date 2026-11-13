namespace ClimateExplorer.WebApi.UnitTests;

using System;
using System.IO;
using ClimateExplorer.DataPipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DataPackageBuilderTests
{
    [TestMethod]
    public void DescribeOmittedFiles_FilesInSeveralFolders_GroupsByFolderWithExamples()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), "SourceData");
        string[] omittedFiles =
        [
            Path.Combine(sourceRoot, "CO2", "co2_daily_mlo.txt"),
            Path.Combine(sourceRoot, "GHCNd", "Temperature", "A.csv"),
            Path.Combine(sourceRoot, "GHCNd", "Temperature", "B.csv"),
            Path.Combine(sourceRoot, "GHCNd", "Temperature", "C.csv"),
            Path.Combine(sourceRoot, "GHCNd", "Temperature", "D.csv"),
        ];

        var message = DataPackageBuilder.DescribeOmittedFiles(sourceRoot, omittedFiles);
        var lines = message.Split(Environment.NewLine);

        Assert.StartsWith("5 source files", lines[0]);
        Assert.AreEqual("  CO2: 1 files (co2_daily_mlo.txt)", lines[1]);
        Assert.AreEqual($"  {Path.Combine("GHCNd", "Temperature")}: 4 files (A.csv, B.csv, C.csv, ...)", lines[2]);
    }
}
