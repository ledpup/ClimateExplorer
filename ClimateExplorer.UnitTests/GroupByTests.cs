using ClimateExplorer.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class GroupByTests
{
    [TestMethod]
    public void GroupByWeekTest()
    {
        var dataSet = GetDataset();

        var groupedData = dataSet.DataRecords.GroupYearByWeek();

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
        var dataSet = GetDataset();

        var groupedData = dataSet.DataRecords.GroupYearByMonth();

        Assert.AreEqual(12, groupedData.Count());
    }

    [TestMethod]
    public void GroupByThirteenDaysTest()
    {
        var dataSet = GetDataset();

        var groupedData = dataSet.DataRecords.GroupYearByDays(13);

        Assert.AreEqual(28, groupedData.Count());
        groupedData.ToList().Take(27).ToList().ForEach(x => Assert.AreEqual(13, x.ToList().Count));
        Assert.AreEqual(14, groupedData.ToList().Last().Count());
    }

    [TestMethod]
    public void GroupByWeekCheckForDuplicateDayTest()
    {
        var dataSet = GetDataset();

        dataSet.DataRecords[0] = new DataRecord(new DateTime(2021, 1, 2), null);

        Assert.ThrowsException<Exception>(() => dataSet.DataRecords.GroupYearByWeek());
    }

    [TestMethod]
    public void GroupByWeekCheckForTooManyRecordsTest()
    {
        var dataSet = GetDataset();

        dataSet.DataRecords.Add(new DataRecord(new DateTime(2021, 1, 1), null));

        Assert.ThrowsException<Exception>(() => dataSet.DataRecords.GroupYearByWeek());
    }

    [TestMethod]
    public void GroupByWeekCheckForTooFewRecordsTest()
    {
        var dataSet = GetDataset();

        dataSet.DataRecords.Remove(dataSet.DataRecords.Last());

        Assert.ThrowsException<Exception>(() => dataSet.DataRecords.GroupYearByWeek());
    }

    [TestMethod]
    public void GroupByWeekCheckForDifferentYearsTest()
    {
        var dataSet = GetDataset();

        dataSet.DataRecords[0] = new DataRecord(new DateTime(2022, 1, 2), null);

        Assert.ThrowsException<Exception>(() => dataSet.DataRecords.GroupYearByWeek());
    }

    private static DataSet GetDataset()
    {
        var dataSet = new DataSet() { Resolution = Core.Enums.DataResolution.Daily, Year = 2021 };
        var date = new DateTime(2021, 1, 1);
        for (var i = 0; i < 365; i++)
        {
            dataSet.DataRecords.Add(new DataRecord(date, null));
            date = date.AddDays(1);
        }

        return dataSet;
    }
}
