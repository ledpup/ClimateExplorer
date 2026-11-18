namespace ClimateExplorer.Web.UnitTests;

using ClimateExplorer.Web.UiLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AbbreviationTooltipTests
{
    [TestMethod]
    public void Build_NoAbbreviations_ReturnsNull()
    {
        Assert.IsNull(AbbreviationTooltip.Build("Geelong (Avalon Airport)"));
    }

    [TestMethod]
    public void Build_Abbreviations_OneLineEach()
    {
        var tooltip = AbbreviationTooltip.Build("Wollongong (Bellambi AWS)");

        Assert.AreEqual("<p><b>AWS</b>: Automatic weather station</p>", tooltip);
    }

    [TestMethod]
    public void Build_SeveralAbbreviations_SeparatedByLineBreaks()
    {
        var tooltip = AbbreviationTooltip.Build("Calgary Int'l CS");

        Assert.AreEqual("<p><b>Int&#39;l</b>: International<br><b>CS</b>: Climate station</p>", tooltip);
    }
}
