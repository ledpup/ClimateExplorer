namespace ClimateExplorer.UnitTests;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Web.UiLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The JSON the chart datasets serialise to must match what Blazorise.Charts used to send to Chart.js,
/// so the in-house ChartJsChart renders the same chart.
/// </summary>
[TestClass]
public class ChartLogicDatasetTests
{
    // Blazor JS interop serialises with the web defaults (camelCase property names).
    private static readonly JsonSerializerOptions JsInteropOptions = new(JsonSerializerDefaults.Web);

    [TestMethod]
    public void GetLineChartDataset_SmallPoints_SerialisesLineDatasetProperties()
    {
        var dataset = ChartLogic.GetLineChartDataset("Hobart", [1.5, null], "#FF2D2D", UnitOfMeasure.DegreesCelsius, SeriesTransformations.Identity, null, renderSmallPoints: true, SeriesAggregationOptions.Mean);

        var json = Serialise(dataset);

        Assert.AreEqual("line", (string?)json["type"]);
        Assert.AreEqual("degreesCelsius", (string?)json["yAxisID"]);
        Assert.AreEqual(4, (int?)json["borderWidth"]);
        Assert.AreEqual(0.1f, (float?)json["pointRadius"]);
        Assert.AreEqual("transparent", (string?)json["pointBackgroundColor"]);
        Assert.IsTrue((bool?)json["showLine"]);
        Assert.IsFalse((bool?)json["fill"]);
        Assert.IsEmpty(json["borderDash"]!.AsArray());
        Assert.IsNull(json["data"]![1]);
        Assert.AreEqual("#FF2D2D", (string?)json["borderColor"]![0]);
        Assert.IsFalse(json.ContainsKey("pointHoverRadius"));
        Assert.IsFalse(json.ContainsKey("trendlineLinear"));
    }

    [TestMethod]
    public void GetBarChartDataset_DefaultOptions_OmitsLineOnlyProperties()
    {
        var dataset = ChartLogic.GetBarChartDataset("Rain", [1.5], "#36A2EB", UnitOfMeasure.Millimetres, false, null, null, SeriesTransformations.Identity, null, SeriesAggregationOptions.Sum);

        var json = Serialise(dataset);

        Assert.AreEqual("bar", (string?)json["type"]);
        Assert.AreEqual(0, (int?)json["borderWidth"]);
        CollectionAssert.AreEquivalent(
            new[] { "type", "label", "data", "backgroundColor", "borderColor", "borderWidth", "yAxisID" },
            new List<string>(((IDictionary<string, JsonNode?>)json).Keys));
    }

    [TestMethod]
    public void GetBarChartColourSet_PositiveAndNegativeValues_UsesDefaultSignColoursAndNullForGaps()
    {
        var colours = ChartLogic.GetBarChartColourSet([2, -1, null], "#36A2EB");

        CollectionAssert.AreEqual(new[] { "rgba(255,63,63,1)", "rgba(63,63,255,1)", null }, colours);
    }

    [TestMethod]
    public void GetBarChartColourSet_OnlyPositiveValues_UsesSeriesColour()
    {
        var colours = ChartLogic.GetBarChartColourSet([2, 1], "#36A2EB");

        CollectionAssert.AreEqual(new[] { "#36A2EB", "#36A2EB" }, colours);
    }

    [TestMethod]
    public void CreateTrendline_Colour_SerialisesTrendlinePluginOptions()
    {
        var dataset = ChartLogic.GetTrendChartDataset("Trend", [1.0], "#FF2D2D", "degreesCelsius") with { TrendlineLinear = ChartLogic.CreateTrendline("#FF2D2D") };

        var trendline = Serialise(dataset)["trendlineLinear"]!;

        Assert.AreEqual("#FF2D2D", (string?)trendline["style"]);
        Assert.AreEqual("solid", (string?)trendline["lineStyle"]);
        Assert.AreEqual(3, (int?)trendline["width"]);
    }

    private static JsonObject Serialise(ChartJsDataset dataset)
    {
        return JsonSerializer.SerializeToNode(dataset, JsInteropOptions)!.AsObject();
    }
}
