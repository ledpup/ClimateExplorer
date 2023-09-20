using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class DataReaderTests
{
    [TestMethod]
    public async Task ProcessDataTest()
    {
        string[]? lines = new[]
        {
            "Header",
            "1427,19501230:2100,21.8,24,15.6,24,-,-,-,-,-,D",
            "1427,19501231:2100,24.2,24,18.4,24,-,-,-,-,-,D",
            "1427,19510101:2100,26.4,24,16.2,24,-,-,-,-,-,D",
            "1427,19510102:2100,19.0,24,12.6,24,-,-,-,-,-,D",
            "EOF",
        };

        var regEx = new Regex(@"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$");

        var records = await DataReader.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Daily);
        var recordsList = records.Values.ToList();

        Assert.AreEqual(4, records.Count);
        Assert.AreEqual("1950_12_30", recordsList[0].Key);
        Assert.AreEqual("1951_1_2", recordsList[3].Key);
        Assert.AreEqual(DateTime.Parse("1951/1/2"), records["1951_1_2"].Date);

        Assert.AreEqual(21.8f, records["1950_12_30"].Value);
        Assert.AreEqual(24.2f, records["1950_12_31"].Value);
        Assert.AreEqual(26.4f, records["1951_1_1"].Value);
        Assert.AreEqual(19.0f, records["1951_1_2"].Value);
    }

    [TestMethod]
    public async Task ProcessDataWithNullRecordsTest()
    {
        string[]? lines = new[]
        {
            "Header",
            "1427,19501230:2100,21.8,24,15.6,24,-,-,-,-,-,D",
            "1427,19501231:2100,-,24,18.4,24,-,-,-,-,-,D",
            "1427,19510101:2100,-,24,16.2,24,-,-,-,-,-,D",
            "1427,19510102:2100,19.0,24,12.6,24,-,-,-,-,-,D",
            "EOF",
        };

        var regEx = new Regex(@"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$");

        var records = await DataReader.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Daily);

        Assert.AreEqual(4, records.Count);

        Assert.AreEqual(21.8f, records["1950_12_30"].Value);
        Assert.AreEqual(null, records["1950_12_31"].Value);
        Assert.AreEqual(null, records["1951_1_1"].Value);
        Assert.AreEqual(19.0f, records["1951_1_2"].Value);
    }

    [TestMethod]
    public async Task ProcessDataWithMissingRecordsTest()
    {
        string[]? lines = new[]
        {
            "Header",
            "1427,19501230:2100,21.8,24,15.6,24,-,-,-,-,-,D",
            "MISSING",
            "MISSING",
            "1427,19510102:2100,19.0,24,12.6,24,-,-,-,-,-,D",
            "EOF",
        };

        var regEx = new Regex(@"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$");

        var records = await DataReader.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Daily);

        Assert.AreEqual(4, records.Count);

        Assert.AreEqual(21.8f, records["1950_12_30"].Value);
        Assert.AreEqual(null, records["1950_12_31"].Value);
        Assert.AreEqual(null, records["1951_1_1"].Value);
        Assert.AreEqual(19.0f, records["1951_1_2"].Value);
    }

    [TestMethod]
    public async Task ProcessDataFileWithDateFilterTest()
    {
        string[]? lines = new[]
        {
            "Header",
            "1427,19501230:2100,21.8,24,15.6,24,-,-,-,-,-,D",
            "1427,19501231:2100,24.2,24,18.4,24,-,-,-,-,-,D",
            "1427,19510101:2100,26.4,24,16.2,24,-,-,-,-,-,D",
            "1427,19510102:2100,19.0,24,12.6,24,-,-,-,-,-,D",
            "EOF",
        };

        var regEx = new Regex(@"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$");

        var start = DateTime.Parse("1950/12/31");
        var end = DateTime.Parse("1951/1/1");

        var records = await DataReader.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Daily, start, end);

        Assert.AreEqual(2, records.Count);

        Assert.AreEqual(24.2f, records["1950_12_31"].Value);
        Assert.AreEqual(26.4f, records["1951_1_1"].Value);
    }

    [TestMethod]
    public async Task ProcessMonthlyDataTest()
    {
        string[]? lines = new[]
        {
            "Header",
            "19400801 19400831    18.7",
            "19400901 19400930    19.5",
            "19401001 19401031    19.7",
            "19401101 19401130    21.1",
            "EOF",
        };

        var regEx = new Regex(@"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$");

        var records = await DataReader.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Monthly);
        var recordsList = records.Values.ToList();

        Assert.AreEqual(4, records.Count);
        Assert.AreEqual("1940_8", recordsList[0].Key);
        Assert.AreEqual("1940_11", recordsList[3].Key);
        Assert.AreEqual(null, records["1940_11"].Date);

        Assert.AreEqual(18.7f, records["1940_8"].Value);
        Assert.AreEqual(19.5f, records["1940_9"].Value);
        Assert.AreEqual(19.7f, records["1940_10"].Value);
        Assert.AreEqual(21.1f, records["1940_11"].Value);
    }

    [TestMethod]
    public async Task ProcessMonthlyDataWithMissingRecordsTest()
    {
        string[]? lines = new[]
        {
            "Header",
            "19400801 19400831    18.7",
            "MISSING",
            "19401001 19401031    19.7",
            "19401101 19401130    21.1",
            "EOF",
        };

        var regEx = new Regex(@"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$");

        var records = await DataReader.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Monthly);
        var recordsList = records.Values.ToList();

        Assert.AreEqual(4, records.Count);
        Assert.AreEqual("1940_8", recordsList[0].Key);
        Assert.AreEqual("1940_11", recordsList[3].Key);

        Assert.AreEqual(18.7f, records["1940_8"].Value);
        Assert.AreEqual(null, records["1940_9"].Value);
        Assert.AreEqual(19.7f, records["1940_10"].Value);
        Assert.AreEqual(21.1f, records["1940_11"].Value);
    }

    [TestMethod]
    public async Task ProcessMonthlyDataWithDuplicateRecordsTest()
    {
        string[]? lines = new[]
        {
            "Header",
            "19400801 19400831    18.7",
            "19400801 19400831    18.7", // Duplicate
            "19400901 19400930    19.5",
            "19401001 19401031    19.7",
            "EOF",
        };

        var regEx = new Regex(@"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$");

        var records = await DataReader.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Monthly);
        var recordsList = records.Values.ToList();

        Assert.AreEqual(3, records.Count);
        Assert.AreEqual("1940_8", recordsList[0].Key);
        Assert.AreEqual("1940_10", recordsList[2].Key);

        Assert.AreEqual(18.7f, records["1940_8"].Value);
        Assert.AreEqual(19.5f, records["1940_9"].Value);
        Assert.AreEqual(19.7f, records["1940_10"].Value);
    }

    [TestMethod]
    public async Task ProcessMonthlyDataWithRecordsOutOfOrderTest()
    {
        string[]? lines = new[]
        {
            "Header",
            "19400901 19400930    19.5",
            "19400801 19400831    18.7", // Will be skipped
            "19401001 19401031    19.7",
            "19401101 19401130    21.1",
            "EOF",
        };

        var regEx = new Regex(@"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$");

        var records = await DataReader.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Monthly);
        var recordsList = records.Values.ToList();

        Assert.AreEqual(3, records.Count);
        Assert.AreEqual("1940_9", recordsList[0].Key);
        Assert.AreEqual("1940_11", recordsList[2].Key);

        Assert.AreEqual(19.5f, records["1940_9"].Value);
        Assert.AreEqual(19.7f, records["1940_10"].Value);
    }

    [TestMethod]
    public async Task MultiFileReadAndAdjustTest()
    {
        var dataFileFilterAndAdjustments = new List<DataFileFilterAndAdjustment>()
        {
            new DataFileFilterAndAdjustment
            {
                Id = "1427",
                StartDate = DateTime.Parse("1909/09/01"),
                EndDate = DateTime.Parse("1950/12/31"),
                ValueAdjustment = -0.62f,
            },
            new DataFileFilterAndAdjustment
            {
                Id = "1427",
                StartDate = DateTime.Parse("1951/01/01"),
                EndDate = DateTime.Parse("1976/03/31"),
                ValueAdjustment = -0.65f,
            },
            new DataFileFilterAndAdjustment
            {
                Id = "1945",
                StartDate = DateTime.Parse("1976-04-01"),
                EndDate = DateTime.Parse("1998-07-31"),
                ValueAdjustment = 0.01f
            },
            new DataFileFilterAndAdjustment
            {
                Id = "1962",
                StartDate = DateTime.Parse("1998-08-01T00:00:00"),
                EndDate = null,
                ValueAdjustment = 0
            }
        };

        var regEx = new Regex(@"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$");

        var md = new MeasurementDefinition
        {
            DataAdjustment = Core.Enums.DataAdjustment.Adjusted,
            DataType = Core.Enums.DataType.TempMax,
            DataResolution = Core.Enums.DataResolution.Daily,
            NullValue = "-",
            DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$",
            FolderName = @"Auckland",
            FileNameFormat = "[station].csv"
        };

        await DataReader.GetDataRecords(md, dataFileFilterAndAdjustments);
    }
}
