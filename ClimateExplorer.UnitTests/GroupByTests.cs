using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class GroupByTests
{
    [TestMethod]
    public void GroupByWeekTest()
    {
        var dataRecords = GetDataRecords();

        var groupedData = dataRecords.GroupYearByWeek();

        foreach (var group in groupedData)
        {
            if (group.Key < 51)
            {
                Assert.AreEqual(7, group.ToList().Count);
            }
            else
            {
                Assert.AreEqual(8, group.ToList().Count);
            }
        }
    }

    [TestMethod]
    public void GroupByMonthTest()
    {
        var dataRecords = GetDataRecords();

        var groupedData = dataRecords.GroupYearByMonth();

        Assert.AreEqual(12, groupedData.Count());
    }

    [TestMethod]
    public void GroupByThirteenDaysTest()
    {
        var dataRecords = GetDataRecords();

        var groupedData = dataRecords.GroupYearByDays(13);

        Assert.AreEqual(28, groupedData.Count());
        groupedData.ToList().Take(27).ToList().ForEach(x => Assert.AreEqual(13, x.ToList().Count));
        Assert.AreEqual(14, groupedData.ToList().Last().Count());
    }

    [TestMethod]
    public void GroupByWeekCheckForDuplicateDayTest()
    {
        var dataRecords = GetDataRecords();

        dataRecords[0] = new DataRecord(new DateOnly(2021, 1, 2), null);

        Assert.ThrowsException<Exception>(() => dataRecords.GroupYearByWeek());
    }

    [TestMethod]
    public void GroupByWeekCheckForTooManyRecordsTest()
    {
        var dataRecords = GetDataRecords();

        dataRecords.Add(new DataRecord(new DateOnly(2021, 1, 1), null));

        Assert.ThrowsException<Exception>(() => dataRecords.GroupYearByWeek());
    }

    [TestMethod]
    public void GroupByWeekCheckForTooFewRecordsTest()
    {
        var dataRecords = GetDataRecords();

        dataRecords.Remove(dataRecords.Last());

        Assert.ThrowsException<Exception>(() => dataRecords.GroupYearByWeek());
    }

    [TestMethod]
    public void GroupByWeekCheckForDifferentYearsTest()
    {
        var dataRecords = GetDataRecords();

        dataRecords[0] = new DataRecord(new DateOnly(2022, 1, 2), null);

        Assert.ThrowsException<Exception>(() => dataRecords.GroupYearByWeek());
    }

    private static List<DataRecord> GetDataRecords()
    {
        var dataRecords = new List<DataRecord>();
        var date = new DateOnly(2021, 1, 1);
        for (var i = 0; i < 365; i++)
        {
            dataRecords.Add(new DataRecord(date, null));
            date = date.AddDays(1);
        }

        return dataRecords;
    }
}
