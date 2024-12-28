using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class DataReaderTests
{
    [TestMethod]
    public void ProcessDataTest()
    {
        string[]? lines =
        [
            "Header",
            "1427,19501230:2100,21.8,24,15.6,24,-,-,-,-,-,D",
            "1427,19501231:2100,24.2,24,18.4,24,-,-,-,-,-,D",
            "1427,19510101:2100,26.4,24,16.2,24,-,-,-,-,-,D",
            "1427,19510102:2100,19.0,24,12.6,24,-,-,-,-,-,D",
            "EOF",
        ];

        var regEx = new Regex(@"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$");

        var records = DataReaderFunctions.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Daily, string.Empty);
        var recordsList = records.Values.ToList();

        Assert.AreEqual(4, records.Count);
        Assert.AreEqual("1950_12_30", recordsList[0].Key);
        Assert.AreEqual("1951_1_2", recordsList[3].Key);
        Assert.AreEqual(DateTime.Parse("1951/1/2"), records["1951_1_2"].Date);

        Assert.AreEqual(21.8d, records["1950_12_30"].Value);
        Assert.AreEqual(24.2d, records["1950_12_31"].Value);
        Assert.AreEqual(26.4d, records["1951_1_1"].Value);
        Assert.AreEqual(19.0d, records["1951_1_2"].Value);
    }

    [TestMethod]
    public void ProcessDataWithNullRecordsTest()
    {
        string[]? lines =
        [
            "Header",
            "1427,19501230:2100,21.8,24,15.6,24,-,-,-,-,-,D",
            "1427,19501231:2100,-,24,18.4,24,-,-,-,-,-,D",
            "1427,19510101:2100,-,24,16.2,24,-,-,-,-,-,D",
            "1427,19510102:2100,19.0,24,12.6,24,-,-,-,-,-,D",
            "EOF",
        ];

        var regEx = new Regex(@"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$");

        var records = DataReaderFunctions.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Daily, string.Empty);

        Assert.AreEqual(4, records.Count);

        Assert.AreEqual(21.8d, records["1950_12_30"].Value);
        Assert.AreEqual(null, records["1950_12_31"].Value);
        Assert.AreEqual(null, records["1951_1_1"].Value);
        Assert.AreEqual(19.0d, records["1951_1_2"].Value);
    }

    [TestMethod]
    public void ProcessDataWithMissingRecordsTest()
    {
        string[]? lines =
        [
            "Header",
            "1427,19501230:2100,21.8,24,15.6,24,-,-,-,-,-,D",
            "MISSING",
            "MISSING",
            "1427,19510102:2100,19.0,24,12.6,24,-,-,-,-,-,D",
            "EOF",
        ];

        var regEx = new Regex(@"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$");

        var records = DataReaderFunctions.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Daily, string.Empty);

        Assert.AreEqual(4, records.Count);

        Assert.AreEqual(21.8d, records["1950_12_30"].Value);
        Assert.AreEqual(null, records["1950_12_31"].Value);
        Assert.AreEqual(null, records["1951_1_1"].Value);
        Assert.AreEqual(19.0d, records["1951_1_2"].Value);
    }

    [TestMethod]
    public void ProcessDataFileWithDateFilterTest()
    {
        string[]? lines =
        [
            "Header",
            "1427,19501230:2100,21.8,24,15.6,24,-,-,-,-,-,D",
            "1427,19501231:2100,24.2,24,18.4,24,-,-,-,-,-,D",
            "1427,19510101:2100,26.4,24,16.2,24,-,-,-,-,-,D",
            "1427,19510102:2100,19.0,24,12.6,24,-,-,-,-,-,D",
            "EOF",
        ];

        var regEx = new Regex(@"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$");

        var start = DateTime.Parse("1950/12/31");
        var end = DateTime.Parse("1951/1/1");

        var records = DataReaderFunctions.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Daily, string.Empty, start, end);

        Assert.AreEqual(2, records.Count);

        Assert.AreEqual(24.2D, records["1950_12_31"].Value);
        Assert.AreEqual(26.4D, records["1951_1_1"].Value);
    }

    [TestMethod]
    public void ProcessMonthlyDataTest()
    {
        string[]? lines =
        [
            "Header",
            "19400801 19400831    18.7",
            "19400901 19400930    19.5",
            "19401001 19401031    19.7",
            "19401101 19401130    21.1",
            "EOF",
        ];

        var regEx = new Regex(@"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$");

        var records = DataReaderFunctions.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Monthly, string.Empty);
        var recordsList = records.Values.ToList();

        Assert.AreEqual(4, records.Count);
        Assert.AreEqual("1940_8", recordsList[0].Key);
        Assert.AreEqual("1940_11", recordsList[3].Key);
        Assert.AreEqual(null, records["1940_11"].Date);

        Assert.AreEqual(18.7d, records["1940_8"].Value);
        Assert.AreEqual(19.5d, records["1940_9"].Value);
        Assert.AreEqual(19.7d, records["1940_10"].Value);
        Assert.AreEqual(21.1d, records["1940_11"].Value);
    }

    [TestMethod]
    public void ProcessMonthlyDataWithMissingRecordsTest()
    {
        string[]? lines =
        [
            "Header",
            "19400801 19400831    18.7",
            "MISSING",
            "19401001 19401031    19.7",
            "19401101 19401130    21.1",
            "EOF",
        ];

        var regEx = new Regex(@"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$");

        var records = DataReaderFunctions.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Monthly, string.Empty);
        var recordsList = records.Values.ToList();

        Assert.AreEqual(4, records.Count);
        Assert.AreEqual("1940_8", recordsList[0].Key);
        Assert.AreEqual("1940_11", recordsList[3].Key);

        Assert.AreEqual(18.7d, records["1940_8"].Value);
        Assert.AreEqual(null, records["1940_9"].Value);
        Assert.AreEqual(19.7d, records["1940_10"].Value);
        Assert.AreEqual(21.1d, records["1940_11"].Value);
    }

    [TestMethod]
    public void ProcessMonthlyDataWithDuplicateRecordsTest()
    {
        string[]? lines =
        [
            "Header",
            "19400801 19400831    18.7",
            "19400801 19400831    18.7", // Duplicate
            "19400901 19400930    19.5",
            "19401001 19401031    19.7",
            "EOF",
        ];

        var regEx = new Regex(@"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$");

        var records = DataReaderFunctions.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Monthly, string.Empty);
        var recordsList = records.Values.ToList();

        Assert.AreEqual(3, records.Count);
        Assert.AreEqual("1940_8", recordsList[0].Key);
        Assert.AreEqual("1940_10", recordsList[2].Key);

        Assert.AreEqual(18.7d, records["1940_8"].Value);
        Assert.AreEqual(19.5d, records["1940_9"].Value);
        Assert.AreEqual(19.7d, records["1940_10"].Value);
    }

    [TestMethod]
    public void ProcessMonthlyDataWithRecordsOutOfOrderTest()
    {
        string[]? lines =
        [
            "Header",
            "19400901 19400930    19.5",
            "19400801 19400831    18.7", // Will be skipped
            "19401001 19401031    19.7",
            "19401101 19401130    21.1",
            "EOF",
        ];

        var regEx = new Regex(@"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$");

        var records = DataReaderFunctions.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Monthly, string.Empty);
        var recordsList = records.Values.ToList();

        Assert.AreEqual(3, records.Count);
        Assert.AreEqual("1940_9", recordsList[0].Key);
        Assert.AreEqual("1940_11", recordsList[2].Key);

        Assert.AreEqual(19.5d, records["1940_9"].Value);
        Assert.AreEqual(19.7d, records["1940_10"].Value);
    }

    [TestMethod]
    public void ProcessYearlyDataTest()
    {
        string[]? lines =
        [
            "Header",
            "\"Australia\",\"AUS\",36,1803,0,,0,0,,0,,",
            "\"Australia\",\"AUS\",36,1804,0,,0,0,,0,,",
            "\"Australia\",\"AUS\",36,1805,0.000236,0.000236,0,0,,0,,",
            "\"Australia\",\"AUS\",36,1806,0.000661,0.000661,0,0,,0,,",
            "\"Global\",\"WLD\",756,1988,22076.769322,8892.934287,8960.304520,3466.495212,482.360190,185.512163,,4.301540",
            "\"Global\",\"WLD\",756,1989,22378.778617,8891.691526,9116.432093,3640.650191,492.612225,150.683382,,4.284082",
            "\"Global\",\"WLD\",756,1990,22752.649114,8701.799477,9245.112051,3830.345091,493.565461,258.802642,223.024393,4.279890",
            "\"Global\",\"WLD\",756,1991,23229.424559,8614.591441,9708.487194,3909.326426,507.413180,274.011689,215.594629,4.296775",
            "\"Global\",\"WLD\",756,1992,22567.119570,8409.501692,9211.264628,3959.732615,528.464970,241.983808,216.171857,4.108576",
            "\"Global\",\"WLD\",756,1993,22798.269517,8511.262954,9218.422025,4067.149925,551.190066,234.102199,216.142348,4.087591",
            "\"Global\",\"WLD\",756,1994,23034.638495,8559.118996,9254.104133,4101.934082,588.277980,314.820766,216.382540,4.069201",
            "\"Global\",\"WLD\",756,1995,23524.361461,8803.171255,9338.964092,4210.041956,622.152180,326.479046,223.552931,4.096023",
            "\"Global\",\"WLD\",756,1996,24249.971916,9024.367871,9621.536738,4396.569850,634.919641,350.563692,222.014124,4.162981",
            "\"Global\",\"WLD\",756,1997,24395.796139,8977.736981,9767.216134,4413.479090,660.690751,352.942116,223.731067,4.130343",
            "\"Global\",\"WLD\",756,1998,24330.768589,8735.932385,9893.816307,4485.610753,658.172190,336.677532,220.559423,4.063721",
            "\"Global\",\"WLD\",756,1999,24833.433836,8862.755670,10135.524759,4595.318074,690.450942,330.129361,219.255030,4.092687",
            "EOF",
        ];

        var regEx = new Regex(@"^\""(?<station>\w+)\"",.*,(?<year>\d{4}),(?<value>\d+\.\d+),.*$");

        var records = DataReaderFunctions.ProcessDataFile(lines, regEx, "-", Core.Enums.DataResolution.Yearly, "Global");
        var recordsList = records.Values.ToList();

        Assert.AreEqual(12, records.Count);
        Assert.AreEqual("1997_12_31", recordsList[9].Key);
        Assert.AreEqual("1999_12_31", recordsList[11].Key);

        Assert.AreEqual(23229.424559D, records["1991_12_31"].Value);
        Assert.AreEqual(24395.796139D, records["1997_12_31"].Value);
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
            },
            new DataFileFilterAndAdjustment
            {
                Id = "1427",
                StartDate = DateTime.Parse("1951/01/01"),
                EndDate = DateTime.Parse("1976/03/31"),
            },
            new DataFileFilterAndAdjustment
            {
                Id = "1945",
                StartDate = DateTime.Parse("1976-04-01"),
                EndDate = DateTime.Parse("1998-07-31"),
            },
            new DataFileFilterAndAdjustment
            {
                Id = "1962",
                StartDate = DateTime.Parse("1998-08-01T00:00:00"),
                EndDate = null,
            }
        };

        var regEx = new Regex(@"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$");

        var md = new MeasurementDefinition
        {
            DataAdjustment = Core.Enums.DataAdjustment.Adjusted,
            DataType = Core.Enums.DataType.TempMax,
            UnitOfMeasure = Core.Enums.UnitOfMeasure.DegreesCelsius,
            DataResolution = Core.Enums.DataResolution.Daily,
            NullValue = "-",
            DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$",
            FolderName = @"Auckland",
            FileNameFormat = "[station].csv"
        };

        await DataReaderFunctions.GetDataRecords(md, dataFileFilterAndAdjustments);
    }
}
