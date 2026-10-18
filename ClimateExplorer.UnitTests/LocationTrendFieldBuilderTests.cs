namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services.Trends;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class LocationTrendFieldBuilderTests
{
    [TestMethod]
    public void Build_Last30Significant_LabelsLast30YearTrend()
    {
        var field = LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: false, last30Significant: true));

        Assert.IsNotNull(field);
        Assert.AreEqual("Last 30 year trend", field.Label);
        Assert.AreEqual("+0.32°C /decade", field.Value);
        StringAssert.Contains(field.Tooltip, "last 30 years of records (1995–2024): +0.32°C /decade");
    }

    [TestMethod]
    public void Build_OnlyFullSignificant_LabelsTrend()
    {
        var field = LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: true, last30Significant: false));

        Assert.IsNotNull(field);
        Assert.AreEqual("Trend", field.Label);
        Assert.AreEqual("+0.14°C /decade", field.Value);
        StringAssert.Contains(field.Tooltip, "full record (1910–2024): +0.14°C /decade");
    }

    [TestMethod]
    public void Build_OnlyFullTrendCalculatedAndSignificant_LabelsTrend()
    {
        var location = TemperatureTrendCalculatorTests.CreateLocation();
        location.TemperatureTrends = [new TrendSummary(1980, 0.014, true)];
        location.LastYear = 2024;

        var field = LocationTrendFieldBuilder.Build(location);

        Assert.AreEqual("Trend", field?.Label);
    }

    [TestMethod]
    public void Build_NeitherSignificant_ReturnsNull()
    {
        Assert.IsNull(LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: false, last30Significant: false)));
    }

    [TestMethod]
    public void Build_NullTrends_ReturnsNull()
    {
        Assert.IsNull(LocationTrendFieldBuilder.Build(TemperatureTrendCalculatorTests.CreateLocation()));
    }

    [TestMethod]
    public void Build_MissingLastYear_ReturnsNull()
    {
        var location = CreateLocation(fullSignificant: true, last30Significant: true);
        location.LastYear = null;

        Assert.IsNull(LocationTrendFieldBuilder.Build(location));
    }

    [TestMethod]
    public void Build_Last30AndFullSignificant_TooltipIncludesFullPeriod()
    {
        var field = LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: true, last30Significant: true));

        Assert.IsNotNull(field);
        Assert.AreEqual("Last 30 year trend", field.Label);
        StringAssert.Contains(field.Tooltip, "<br>Full record (1910–2024): +0.14°C /decade.");
    }

    [TestMethod]
    public void Build_FullNotSignificant_TooltipOmitsFullPeriod()
    {
        var field = LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: false, last30Significant: true));

        Assert.IsNotNull(field);
        Assert.DoesNotContain("Full record", field.Tooltip);
    }

    [TestMethod]
    public void Build_NegativeTrend_FormatsWithoutPlusSign()
    {
        var location = TemperatureTrendCalculatorTests.CreateLocation();
        location.TemperatureTrends = [new TrendSummary(1910, -0.021, true)];
        location.LastYear = 2024;

        Assert.AreEqual("-0.21°C /decade", LocationTrendFieldBuilder.Build(location)?.Value);
    }

    private static Location CreateLocation(bool fullSignificant, bool last30Significant)
    {
        var location = TemperatureTrendCalculatorTests.CreateLocation();
        location.TemperatureTrends =
        [
            new TrendSummary(1910, 0.014, fullSignificant),
            new TrendSummary(1995, 0.032, last30Significant),
        ];
        location.LastYear = 2024;

        return location;
    }
}
