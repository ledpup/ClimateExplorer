using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApi.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ClimateExplorer.WebApi.UnitTests;

[TestClass]
public class CacheJsonOptionsTests
{
    private static Location CreateLocation(double? warmingAnomaly, Coordinates coordinates) =>
        new()
        {
            Id = Guid.Empty,
            Name = "Unknown",
            CountryCode = "UU",
            Coordinates = coordinates,
            WarmingAnomaly = warmingAnomaly,
        };

    private static JsonElement SerializeToElement(Location location) =>
        JsonDocument.Parse(JsonSerializer.Serialize(location, CacheJsonOptions.Create())).RootElement;

    [TestMethod]
    public void Serialize_LocationWithLongPrecisionValues_RoundsConfiguredFields()
    {
        var location = CreateLocation(
            1.4605454545454553,
            new Coordinates { Latitude = 43.36690139770508, Longitude = -8.41919994354248, Elevation = 58.4 });

        var json = JsonSerializer.Serialize(location, CacheJsonOptions.Create());

        StringAssert.Contains(json, "\"WarmingAnomaly\":1.461,");
        StringAssert.Contains(json, "\"Latitude\":43.366901");
        StringAssert.Contains(json, "\"Longitude\":-8.4192");
        StringAssert.Contains(json, "\"Elevation\":58");
        Assert.DoesNotContain("58.4", json);
    }

    [TestMethod]
    public void Serialize_NegativeValueRoundingToZero_WritesZeroNotNegativeZero()
    {
        var location = CreateLocation(-0.0004, new Coordinates { Latitude = -0.0000001, Longitude = 0, Elevation = -0.4 });

        var json = JsonSerializer.Serialize(location, CacheJsonOptions.Create());

        Assert.IsFalse(json.Contains("-0,") || json.Contains("-0}"), json);
        StringAssert.Contains(json, "\"WarmingAnomaly\":0,");
    }

    [TestMethod]
    public void Serialize_NullWarmingAnomalyAndElevation_OmitsThem()
    {
        var location = CreateLocation(null, new Coordinates { Latitude = 1, Longitude = 2, Elevation = null });

        var root = SerializeToElement(location);

        Assert.IsFalse(root.TryGetProperty("WarmingAnomaly", out _));
        Assert.IsFalse(root.GetProperty("Coordinates").TryGetProperty("Elevation", out _));
    }

    [TestMethod]
    public void Serialize_OtherDoubles_AreNotRounded()
    {
        var location = CreateLocation(1, new Coordinates { Latitude = 1, Longitude = 2, Elevation = 3 });
        location.TemperatureTrends = [new TrendSummary(TrendWindow.Full, 1990, 0.123456789, true)];

        var json = JsonSerializer.Serialize(location, CacheJsonOptions.Create());

        StringAssert.Contains(json, "\"Slope\":0.123456789");
    }

    [TestMethod]
    public void Serialize_Location_IsNotIndented()
    {
        var json = JsonSerializer.Serialize(
            CreateLocation(1, new Coordinates { Latitude = 1, Longitude = 2, Elevation = 3 }),
            CacheJsonOptions.Create());

        Assert.DoesNotContain("\n", json);
    }

    [TestMethod]
    public void Serialize_RoundedLocation_DeserializesWithStoredValues()
    {
        var location = CreateLocation(
            1.4605454545454553,
            new Coordinates { Latitude = 43.36690139770508, Longitude = -8.41919994354248, Elevation = 58 });

        var json = JsonSerializer.Serialize(new[] { location }, CacheJsonOptions.Create());
        var roundTripped = JsonSerializer.Deserialize<List<Location>>(json)!;

        Assert.AreEqual(1.461, roundTripped[0].WarmingAnomaly);
        Assert.AreEqual(43.366901, roundTripped[0].Coordinates.Latitude);
        Assert.AreEqual(58, roundTripped[0].Coordinates.Elevation);
    }
}
