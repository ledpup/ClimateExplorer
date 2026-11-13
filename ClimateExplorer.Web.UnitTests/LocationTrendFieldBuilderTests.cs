namespace ClimateExplorer.Web.UnitTests;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.UnitTests.TestData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class LocationTrendFieldBuilderTests
{
    [TestMethod]
    public void Build_Last30Significant_LabelsLast30YearTrend()
    {
        var field = LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: false, last30Significant: true));

        Assert.IsNotNull(field);
        Assert.AreEqual("Last 30 years trend", field.Label);
        Assert.AreEqual("+0.32°C /decade", field.Value);
        StringAssert.Contains(field.Tooltip, ">Last 30 years</td>");
        StringAssert.Contains(field.Tooltip, ">1995–2024</td>");
        StringAssert.Contains(field.Tooltip, ">+0.32°C /decade</td>");
    }

    [TestMethod]
    public void Build_Last30NotSignificantSince1961Significant_LabelsTrendSince1961()
    {
        var field = LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: true, last30Significant: false, since1961Significant: true));

        Assert.IsNotNull(field);
        Assert.AreEqual("Trend since 1961", field.Label);
        Assert.AreEqual("+0.25°C /decade", field.Value);
        Assert.AreEqual(0.025, field.Slope);
        StringAssert.Contains(field.Tooltip, ">Since 1961</td>");
        StringAssert.Contains(field.Tooltip, ">1961–2024</td>");
        StringAssert.Contains(field.Tooltip, ">+0.25°C /decade</td>");
        Assert.DoesNotContain("Last 30 years", field.Tooltip);
    }

    [TestMethod]
    public void Build_AllSignificant_ShowsLast30AndListsAllThreeInOrder()
    {
        var field = LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: true, last30Significant: true, since1961Significant: true));

        Assert.IsNotNull(field);
        Assert.AreEqual("Last 30 years trend", field.Label);

        var last30 = field.Tooltip.IndexOf(">Last 30 years</td>", System.StringComparison.Ordinal);
        var since1961 = field.Tooltip.IndexOf(">Since 1961</td>", System.StringComparison.Ordinal);
        var full = field.Tooltip.IndexOf(">Full period</td>", System.StringComparison.Ordinal);
        Assert.IsTrue(last30 >= 0 && last30 < since1961 && since1961 < full, field.Tooltip);
    }

    [TestMethod]
    public void Build_Since1961NotSignificant_TooltipOmitsIt()
    {
        var field = LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: true, last30Significant: true, since1961Significant: false));

        Assert.IsNotNull(field);
        Assert.DoesNotContain("Since 1961", field.Tooltip);
    }

    [TestMethod]
    public void Build_OnlyFullSignificant_LabelsTrend()
    {
        var field = LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: true, last30Significant: false));

        Assert.IsNotNull(field);
        Assert.AreEqual("Trend", field.Label);
        Assert.AreEqual("+0.14°C /decade", field.Value);
        StringAssert.Contains(field.Tooltip, ">Full period</td>");
        StringAssert.Contains(field.Tooltip, ">1910–2024</td>");
        StringAssert.Contains(field.Tooltip, ">+0.14°C /decade</td>");
    }

    [TestMethod]
    public void Build_OnlyFullTrendCalculatedAndSignificant_LabelsTrend()
    {
        var location = TrendTestData.CreateLocation();
        location.TemperatureTrends = [new TrendSummary(TrendWindow.Full, 1980, 0.014, true)];
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
        Assert.IsNull(LocationTrendFieldBuilder.Build(TrendTestData.CreateLocation()));
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
        Assert.AreEqual("Last 30 years trend", field.Label);
        StringAssert.Contains(field.Tooltip, ">Last 30 years</td>");
        StringAssert.Contains(field.Tooltip, ">Full period</td>");
        StringAssert.Contains(field.Tooltip, ">1910–2024</td>");
        StringAssert.Contains(field.Tooltip, ">+0.14°C /decade</td>");
    }

    [TestMethod]
    public void Build_FullNotSignificant_TooltipOmitsFullPeriod()
    {
        var field = LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: false, last30Significant: true));

        Assert.IsNotNull(field);
        Assert.DoesNotContain("Full period", field.Tooltip);
    }

    [TestMethod]
    public void Build_SignificantTrend_TooltipSaysClickForMore()
    {
        var field = LocationTrendFieldBuilder.Build(CreateLocation(fullSignificant: true, last30Significant: false));

        Assert.IsNotNull(field);
        Assert.EndsWith("Click for information on trends.</p>", field.Tooltip);
    }

    [TestMethod]
    public void Build_NegativeTrend_FormatsWithoutPlusSign()
    {
        var location = TrendTestData.CreateLocation();
        location.TemperatureTrends = [new TrendSummary(TrendWindow.Full, 1910, -0.021, true)];
        location.LastYear = 2024;

        Assert.AreEqual("-0.21°C /decade", LocationTrendFieldBuilder.Build(location)?.Value);
    }

    [TestMethod]
    public void FormatAmountPerDecade_PositiveAndNegativeSlopes_OmitsPeriodSuffix()
    {
        Assert.AreEqual("+0.32°C", LocationTrendFieldBuilder.FormatAmountPerDecade(0.032));
        Assert.AreEqual("-0.21°C", LocationTrendFieldBuilder.FormatAmountPerDecade(-0.021));
    }

    private static Location CreateLocation(bool fullSignificant, bool last30Significant, bool since1961Significant = false)
    {
        var location = TrendTestData.CreateLocation();
        location.TemperatureTrends =
        [
            new TrendSummary(TrendWindow.Full, 1910, 0.014, fullSignificant),
            new TrendSummary(TrendWindow.Last30, 1995, 0.032, last30Significant),
            new TrendSummary(TrendWindow.Since1961, 1961, 0.025, since1961Significant),
        ];
        location.LastYear = 2024;

        return location;
    }
}
