namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DataFileSourceTests
{
    private string datasetsFolder = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        datasetsFolder = Path.Combine(Path.GetTempPath(), $"ClimateExplorerDataFileSourceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(datasetsFolder);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(datasetsFolder))
        {
            Directory.Delete(datasetsFolder, true);
        }
    }

    [TestMethod]
    public async Task GetLinesInDataFileSource_LooseDatasetFile_ReadsFile()
    {
        var sourceFolder = Path.Combine(datasetsFolder, "Met");
        Directory.CreateDirectory(sourceFolder);
        await File.WriteAllLinesAsync(Path.Combine(sourceFolder, "maxtemp_daily_totals.txt"), ["header", "record"]);

        var source = new DataFileSourceDefinition
        {
            FilePathFormat = @"Met\maxtemp_daily_totals.txt",
        };

        var lines = await DataReaderFunctions.GetLinesInDataFileSource(source, string.Empty, datasetsFolder);

        CollectionAssert.AreEqual(new[] { "header", "record" }, lines);
    }

    [TestMethod]
    public async Task GetLinesInDataFileSource_SharedStationArchive_ReadsMeasurementEntry()
    {
        var archivePath = Path.Combine(datasetsFolder, "BOM", "001019.zip");
        CreateArchive(
            archivePath,
            new Dictionary<string, string>
            {
                ["001019_daily_tempmax.csv"] = "max-header\nmax-record",
                ["001019_daily_rainfall.csv"] = "rain-header\nrain-record",
            });

        var source = new DataFileSourceDefinition
        {
            FilePathFormat = @"BOM\[station].zip",
            ArchiveEntryPathFormat = "[station]_daily_rainfall.csv",
        };

        var lines = await DataReaderFunctions.GetLinesInDataFileSource(source, "001019", datasetsFolder);

        CollectionAssert.AreEqual(new[] { "rain-header", "rain-record" }, lines);
    }

    [TestMethod]
    public async Task GetDataRecords_ExplicitSharedStationArchive_ReadsConfiguredMeasurementEntry()
    {
        var archivePath = Path.Combine(datasetsFolder, "BOM", "001019.zip");
        CreateArchive(
            archivePath,
            new Dictionary<string, string>
            {
                ["001019_daily_tempmax.csv"] = "date,value\n202001,31.5",
                ["001019_daily_rainfall.csv"] = "date,value\n202001,14.2",
            });

        var measurementDefinition = new MeasurementDefinition
        {
            DataType = Core.Enums.DataType.Precipitation,
            UnitOfMeasure = Core.Enums.UnitOfMeasure.Millimetres,
            DataResolution = Core.Enums.DataResolution.Monthly,
            DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2}),(?<value>-?\d+(?:\.\d+)?)$",
            NullValue = string.Empty,
            DataFileSource = new DataFileSourceDefinition
            {
                FilePathFormat = @"BOM\[station].zip",
                ArchiveEntryPathFormat = "[station]_daily_rainfall.csv",
            },
        };

        var records = await DataReaderFunctions.GetDataRecords(
            measurementDefinition,
            [new DataFileFilterAndAdjustment { Id = "001019" }],
            datasetsFolder);

        Assert.HasCount(1, records);
        Assert.AreEqual(14.2, records[0].Value);
    }

    [TestMethod]
    public async Task GetLinesInDataFileWithCascade_DatasetArchive_ReadsExistingLayout()
    {
        CreateArchive(
            Path.Combine(datasetsFolder, "Ocean.zip"),
            new Dictionary<string, string>
            {
                ["nino34.long.anom.data.txt"] = "header\nrecord",
            });

        var lines = await DataReaderFunctions.GetLinesInDataFileWithCascade(
            @"Ocean\nino34.long.anom.data.txt",
            datasetsFolder);

        CollectionAssert.AreEqual(new[] { "header", "record" }, lines);
    }

    [TestMethod]
    public async Task GetLinesInDataFileWithCascade_SingleEntryArchive_ReadsExistingLayout()
    {
        CreateArchive(
            Path.Combine(datasetsFolder, "GHCNd", "Temperature", "AE000041196.zip"),
            new Dictionary<string, string>
            {
                ["AE000041196.csv"] = "header\nrecord",
            });

        var lines = await DataReaderFunctions.GetLinesInDataFileWithCascade(
            @"GHCNd\Temperature\AE000041196.csv",
            datasetsFolder);

        CollectionAssert.AreEqual(new[] { "header", "record" }, lines);
    }

    [TestMethod]
    public async Task GetLinesInDataFileWithCascade_ExistingLoosePathOutsideDatasetsFolder_ReadsFile()
    {
        var looseFilePath = Path.Combine(Path.GetDirectoryName(datasetsFolder)!, $"{Guid.NewGuid():N}.txt");

        try
        {
            await File.WriteAllLinesAsync(looseFilePath, ["header", "record"]);

            var lines = await DataReaderFunctions.GetLinesInDataFileWithCascade(looseFilePath, datasetsFolder);

            CollectionAssert.AreEqual(new[] { "header", "record" }, lines);
        }
        finally
        {
            File.Delete(looseFilePath);
        }
    }

    [TestMethod]
    public async Task GetLinesInDataFileSource_UnresolvedPlaceholder_ThrowsInvalidOperationException()
    {
        var source = new DataFileSourceDefinition
        {
            FilePathFormat = @"BOM\[station]\[year].zip",
        };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => DataReaderFunctions.GetLinesInDataFileSource(source, "001019", datasetsFolder));
    }

    [TestMethod]
    public async Task GetLinesInDataFileSource_PathOutsideDatasetsFolder_ThrowsInvalidOperationException()
    {
        var source = new DataFileSourceDefinition
        {
            FilePathFormat = @"..\outside.txt",
        };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => DataReaderFunctions.GetLinesInDataFileSource(source, string.Empty, datasetsFolder));
    }

    private static void CreateArchive(string archivePath, IReadOnlyDictionary<string, string> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);

        foreach (var entry in entries)
        {
            var archiveEntry = archive.CreateEntry(entry.Key);
            using var writer = new StreamWriter(archiveEntry.Open());
            writer.Write(entry.Value);
        }
    }
}
