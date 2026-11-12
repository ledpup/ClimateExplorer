namespace ClimateExplorer.Core.UnitTests;

using System.Text.Json;
using ClimateExplorer.Core.Model;
using ClimateExplorer.UnitTests.TestData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class LocationTemperatureTrendsSerializationTests
{
    [TestMethod]
    public void Serialize_TemperatureTrends_RoundTripsWithDefaultOptions()
    {
        // FileBackedCache - the locations cache - uses default options.
        AssertRoundTrips(JsonSerializerOptions.Default);
    }

    [TestMethod]
    public void Serialize_TemperatureTrends_RoundTripsWithWebOptions()
    {
        // DataService deserialises the API's response with web defaults.
        AssertRoundTrips(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    [TestMethod]
    public void Serialize_TemperatureTrends_OmitsComputedAccessors()
    {
        var json = JsonSerializer.Serialize(CreateLocationWithTrends());

        Assert.DoesNotContain(nameof(Location.FullPeriodTemperatureTrend), json);
        Assert.DoesNotContain(nameof(Location.Last30YearTemperatureTrend), json);
        Assert.DoesNotContain(nameof(Location.Since1961TemperatureTrend), json);
    }

    [TestMethod]
    public void Serialize_TemperatureTrends_WritesWindowByName()
    {
        var json = JsonSerializer.Serialize(CreateLocationWithTrends());

        StringAssert.Contains(json, "\"Window\":\"Full\"");
        StringAssert.Contains(json, "\"Window\":\"Last30\"");
        StringAssert.Contains(json, "\"Window\":\"Since1961\"");
    }

    [TestMethod]
    public void FullPeriodTemperatureTrend_ThreeTrends_ReturnsTrendForEachWindow()
    {
        var location = CreateLocationWithTrends();

        Assert.AreEqual(1910, location.FullPeriodTemperatureTrend!.FirstYear);
        Assert.AreEqual(1995, location.Last30YearTemperatureTrend!.FirstYear);
        Assert.AreEqual(1961, location.Since1961TemperatureTrend!.FirstYear);
    }

    [TestMethod]
    public void FullPeriodTemperatureTrend_TrendsOutOfOrder_SelectsByWindow()
    {
        var location = TrendTestData.CreateLocation();
        location.TemperatureTrends = [new TrendSummary(TrendWindow.Last30, 1995, 0.03456, false), new TrendSummary(TrendWindow.Full, 1910, 0.01234, true)];

        Assert.AreEqual(1910, location.FullPeriodTemperatureTrend!.FirstYear);
        Assert.AreEqual(1995, location.Last30YearTemperatureTrend!.FirstYear);
    }

    [TestMethod]
    public void Last30YearTemperatureTrend_OneTrend_ReturnsNull()
    {
        var location = TrendTestData.CreateLocation();
        location.TemperatureTrends = [new TrendSummary(TrendWindow.Full, 1980, 0.02, true)];
        location.LastYear = 2024;

        Assert.AreEqual(1980, location.FullPeriodTemperatureTrend!.FirstYear);
        Assert.IsNull(location.Last30YearTemperatureTrend);
        Assert.IsNull(location.Since1961TemperatureTrend);
    }

    [TestMethod]
    public void FullPeriodTemperatureTrend_NoTrends_ReturnsNull()
    {
        var location = TrendTestData.CreateLocation();

        Assert.IsNull(location.FullPeriodTemperatureTrend);
        Assert.IsNull(location.Last30YearTemperatureTrend);
        Assert.IsNull(location.Since1961TemperatureTrend);
    }

    private static void AssertRoundTrips(JsonSerializerOptions options)
    {
        var json = JsonSerializer.Serialize(CreateLocationWithTrends(), options);
        var roundTripped = JsonSerializer.Deserialize<Location>(json, options)!;

        Assert.AreEqual(2024, roundTripped.LastYear);
        Assert.HasCount(3, roundTripped.TemperatureTrends!);
        Assert.AreEqual(new TrendSummary(TrendWindow.Full, 1910, 0.01234, true), roundTripped.TemperatureTrends![0]);
        Assert.AreEqual(new TrendSummary(TrendWindow.Last30, 1995, 0.03456, false), roundTripped.TemperatureTrends[1]);
        Assert.AreEqual(new TrendSummary(TrendWindow.Since1961, 1961, 0.02345, true), roundTripped.TemperatureTrends[2]);
    }

    private static Location CreateLocationWithTrends()
    {
        var location = TrendTestData.CreateLocation();
        location.TemperatureTrends =
        [
            new TrendSummary(TrendWindow.Full, 1910, 0.01234, true),
            new TrendSummary(TrendWindow.Last30, 1995, 0.03456, false),
            new TrendSummary(TrendWindow.Since1961, 1961, 0.02345, true),
        ];
        location.LastYear = 2024;

        return location;
    }
}
