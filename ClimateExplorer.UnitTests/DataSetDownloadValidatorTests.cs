namespace ClimateExplorer.UnitTests;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Orchestration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class DataSetDownloadValidatorTests
{
    private string temporaryRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        temporaryRoot = Path.Combine(Path.GetTempPath(), $"ClimateExplorerDownloadValidatorTests_{Guid.NewGuid():N}");
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
    public async Task ValidateAsync_DailyOneValuePerRowMeasurement_ReturnsLatestRecordDate()
    {
        var request = CreateRequest(
            CreateDailyMeasurement("TEMP", "date,value\n20260101,10.0\n20260714,15.0"));

        var result = await new DataSetDownloadValidator().ValidateAsync(request, temporaryRoot, CancellationToken.None);

        Assert.AreEqual(new DateOnly(2026, 7, 14), result);
    }

    [TestMethod]
    public async Task ValidateAsync_MultipleMeasurementsWithDifferingCoverage_ReturnsMinimumOfTheirLatestDates()
    {
        var request = CreateRequest(
            CreateDailyMeasurement("TEMP", "date,value\n20260101,10.0\n20260714,15.0", fileName: "temp.csv"),
            CreateDailyMeasurement("PRCP", "date,value\n20260101,1.0\n20260601,2.0", fileName: "prcp.csv"));

        var result = await new DataSetDownloadValidator().ValidateAsync(request, temporaryRoot, CancellationToken.None);

        Assert.AreEqual(new DateOnly(2026, 6, 1), result);
    }

    [TestMethod]
    public async Task ValidateAsync_NonDailyOrTwelveMonthsPerRow_ReturnsNull()
    {
        var request = CreateRequest(CreateMonthlyMeasurement("TEMP", "date,value\n202601,10.0\n202607,15.0"));

        var result = await new DataSetDownloadValidator().ValidateAsync(request, temporaryRoot, CancellationToken.None);

        Assert.IsNull(result);
    }

    private static DataSetDownloadRequest CreateRequest(params DataSetDownloadMeasurement[] measurements)
    {
        return new DataSetDownloadRequest(
            new DataSetDefinition { Id = Guid.NewGuid(), Name = "Test", ShortName = "TEST" },
            "test-downloader",
            "asset-key",
            measurements[0].FileFilter.Id!,
            null,
            measurements);
    }

    private DataSetDownloadMeasurement CreateDailyMeasurement(string dataType, string csvContent, string fileName = "data.csv")
    {
        File.WriteAllText(Path.Combine(temporaryRoot, fileName), csvContent);

        var measurementDefinition = new MeasurementDefinition
        {
            DataType = DataType.TempMax,
            UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
            DataResolution = DataResolution.Daily,
            RowDataType = RowDataType.OneValuePerRow,
            DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}),(?<value>-?\d+(?:\.\d+)?)$",
            NullValue = string.Empty,
            DataFileSource = new DataFileSourceDefinition { FilePathFormat = fileName },
        };

        return new DataSetDownloadMeasurement(measurementDefinition, new DataFileFilterAndAdjustment { Id = dataType });
    }

    private DataSetDownloadMeasurement CreateMonthlyMeasurement(string dataType, string csvContent, string fileName = "data.csv")
    {
        File.WriteAllText(Path.Combine(temporaryRoot, fileName), csvContent);

        var measurementDefinition = new MeasurementDefinition
        {
            DataType = DataType.TempMax,
            UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
            DataResolution = DataResolution.Monthly,
            RowDataType = RowDataType.OneValuePerRow,
            DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2}),(?<value>-?\d+(?:\.\d+)?)$",
            NullValue = string.Empty,
            DataFileSource = new DataFileSourceDefinition { FilePathFormat = fileName },
        };

        return new DataSetDownloadMeasurement(measurementDefinition, new DataFileFilterAndAdjustment { Id = dataType });
    }
}
