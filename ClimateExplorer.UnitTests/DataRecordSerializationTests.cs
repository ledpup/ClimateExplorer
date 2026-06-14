namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

[TestClass]
public class DataRecordSerializationTests
{
    [TestMethod]
    public void DeserializeDataRecord()
    {
        var json = """
            {
                "Year": 2024,
                "Month": 2,
                "Day": 29,
                "Value": 12.34
            }
            """;

        var record = JsonSerializer.Deserialize<DataRecord>(json)!;

        Assert.AreEqual((short)2024, record.Year);
        Assert.AreEqual((short)2, record.Month);
        Assert.AreEqual((short)29, record.Day);
        Assert.AreEqual(12.34d, record.Value);
        Assert.AreEqual("2024_2_29", record.Key);
        Assert.AreEqual(new DateOnly(2024, 2, 29), record.Date);
    }
}
