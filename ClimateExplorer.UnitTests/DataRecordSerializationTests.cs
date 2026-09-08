namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

[TestClass]
public class DataRecordSerializationTests
{
    [TestMethod]
    public void DeserializeDataRecord_Daily()
    {
        var json = "[20240229,12.34]";

        var record = JsonSerializer.Deserialize<DataRecord>(json)!;

        Assert.AreEqual((short)2024, record.Year);
        Assert.AreEqual((short)2, record.Month);
        Assert.AreEqual((short)29, record.Day);
        Assert.AreEqual(12.34d, record.Value);
        Assert.AreEqual(new DateOnly(2024, 2, 29), record.Date);
    }

    [TestMethod]
    public void DeserializeDataRecord_Monthly()
    {
        var json = "[202402,12.34]";

        var record = JsonSerializer.Deserialize<DataRecord>(json)!;

        Assert.AreEqual((short)2024, record.Year);
        Assert.AreEqual((short)2, record.Month);
        Assert.IsNull(record.Day);
        Assert.AreEqual(12.34d, record.Value);
        Assert.IsNull(record.Date);
    }

    [TestMethod]
    public void DeserializeDataRecord_Yearly()
    {
        var json = "[2024,12.34]";

        var record = JsonSerializer.Deserialize<DataRecord>(json)!;

        Assert.AreEqual((short)2024, record.Year);
        Assert.IsNull(record.Month);
        Assert.IsNull(record.Day);
        Assert.AreEqual(12.34d, record.Value);
    }

    [TestMethod]
    public void DeserializeDataRecord_NullValue()
    {
        var json = "[20240229,null]";

        var record = JsonSerializer.Deserialize<DataRecord>(json)!;

        Assert.AreEqual((short)2024, record.Year);
        Assert.AreEqual((short)2, record.Month);
        Assert.AreEqual((short)29, record.Day);
        Assert.IsNull(record.Value);
    }

    [TestMethod]
    public void SerializeDataRecord_Daily()
    {
        var record = new DataRecord((short)2024, (short)2, (short)29, 12.34d);

        var json = JsonSerializer.Serialize(record);

        Assert.AreEqual("[20240229,12.34]", json);
    }

    [TestMethod]
    public void SerializeDataRecord_Monthly()
    {
        var record = new DataRecord((short)2024, (short)2, null);

        var json = JsonSerializer.Serialize(record);

        Assert.AreEqual("[202402,null]", json);
    }

    [TestMethod]
    public void SerializeDataRecord_Yearly()
    {
        var record = new DataRecord((short)2024, null, null, null);

        var json = JsonSerializer.Serialize(record);

        Assert.AreEqual("[2024,null]", json);
    }

    [TestMethod]
    public void RoundTripsThroughSerialization()
    {
        var original = new DataRecord((short)2024, (short)2, (short)29, 12.34d);

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<DataRecord>(json);

        Assert.AreEqual(original, roundTripped);
    }
}
