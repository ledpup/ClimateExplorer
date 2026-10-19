namespace ClimateExplorer.UnitTests;

using System.Text.Json;
using ClimateExplorer.Core.Model;
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
    }

    [TestMethod]
    public void Serialize_TemperatureTrends_WritesWindowByName()
    {
        var json = JsonSerializer.Serialize(CreateLocationWithTrends());

        StringAssert.Contains(json, "\"Window\":\"Full\"");
        StringAssert.Contains(json, "\"Window\":\"Last30\"");
    }

    [TestMethod]
    public void FullPeriodTemperatureTrend_TwoTrends_ReturnsTrendForEachWindow()
    {
        var location = CreateLocationWithTrends();

        Assert.AreEqual(1910, location.FullPeriodTemperatureTrend!.FirstYear);
        Assert.AreEqual(1995, location.Last30YearTemperatureTrend!.FirstYear);
    }

    [TestMethod]
    public void FullPeriodTemperatureTrend_TrendsOutOfOrder_SelectsByWindow()
    {
        var location = TemperatureTrendCalculatorTests.CreateLocation();
        location.TemperatureTrends = [new TrendSummary(TrendWindow.Last30, 1995, 0.03456, false), new TrendSummary(TrendWindow.Full, 1910, 0.01234, true)];

        Assert.AreEqual(1910, location.FullPeriodTemperatureTrend!.FirstYear);
        Assert.AreEqual(1995, location.Last30YearTemperatureTrend!.FirstYear);
    }

    [TestMethod]
    public void Last30YearTemperatureTrend_OneTrend_ReturnsNull()
    {
        var location = TemperatureTrendCalculatorTests.CreateLocation();
        location.TemperatureTrends = [new TrendSummary(TrendWindow.Full, 1980, 0.02, true)];
        location.LastYear = 2024;

        Assert.AreEqual(1980, location.FullPeriodTemperatureTrend!.FirstYear);
        Assert.IsNull(location.Last30YearTemperatureTrend);
    }

    [TestMethod]
    public void FullPeriodTemperatureTrend_NoTrends_ReturnsNull()
    {
        var location = TemperatureTrendCalculatorTests.CreateLocation();

        Assert.IsNull(location.FullPeriodTemperatureTrend);
        Assert.IsNull(location.Last30YearTemperatureTrend);
    }

    private static void AssertRoundTrips(JsonSerializerOptions options)
    {
        var json = JsonSerializer.Serialize(CreateLocationWithTrends(), options);
        var roundTripped = JsonSerializer.Deserialize<Location>(json, options)!;

        Assert.AreEqual(2024, roundTripped.LastYear);
        Assert.HasCount(2, roundTripped.TemperatureTrends!);
        Assert.AreEqual(new TrendSummary(TrendWindow.Full, 1910, 0.01234, true), roundTripped.TemperatureTrends![0]);
        Assert.AreEqual(new TrendSummary(TrendWindow.Last30, 1995, 0.03456, false), roundTripped.TemperatureTrends[1]);
    }

    private static Location CreateLocationWithTrends()
    {
        var location = TemperatureTrendCalculatorTests.CreateLocation();
        location.TemperatureTrends = [new TrendSummary(TrendWindow.Full, 1910, 0.01234, true), new TrendSummary(TrendWindow.Last30, 1995, 0.03456, false)];
        location.LastYear = 2024;

        return location;
    }
}
