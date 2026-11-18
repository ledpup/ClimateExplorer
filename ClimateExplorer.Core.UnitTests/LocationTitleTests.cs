using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ClimateExplorer.Core.UnitTests;

[TestClass]
public class LocationTitleTests
{
    [TestMethod]
    public void FullTitle_WithStationName_PutsStationInBracketsAfterName()
    {
        var location = CreateLocation("Geelong", "Avalon Airport");

        Assert.AreEqual("Geelong (Avalon Airport), Australia", location.FullTitle);
    }

    [TestMethod]
    public void FullTitle_WithoutStationName_IsNameAndCountry()
    {
        var location = CreateLocation("Laverton", null);

        Assert.AreEqual("Laverton, Australia", location.FullTitle);
    }

    [TestMethod]
    public void DisplayTitle_WithStationNameOverMaximumLength_UsesCountryCode()
    {
        // "Geelong (Avalon Airport), Australia" is 35 characters, over the 30 allowed.
        var location = CreateLocation("Geelong", "Avalon Airport");

        Assert.AreEqual("Geelong (Avalon Airport), AU", location.DisplayTitle);
    }

    [TestMethod]
    public void DisplayTitle_WithShortStationName_UsesCountryName()
    {
        var location = CreateLocation("Ayr", "Ayr Dpi");

        Assert.AreEqual("Ayr (Ayr Dpi), Australia", location.DisplayTitle);
    }

    [TestMethod]
    public void CodeTitle_WithStationName_LeavesStationOut()
    {
        var location = CreateLocation("Geelong", "Avalon Airport");

        Assert.AreEqual("Geelong, AU", location.CodeTitle);
    }

    [TestMethod]
    public void UrlReadyName_WithStationName_UsesTheNameOnly()
    {
        var location = CreateLocation("Geelong", "Avalon Airport");

        Assert.AreEqual("geelong", location.UrlReadyName());
    }

    [TestMethod]
    public void ToDisplayName_UpperCaseWithUnderscores_ReturnsTitleCaseWithSpaces()
    {
        Assert.AreEqual("Avalon Airport", Station.ToDisplayName("AVALON_AIRPORT"));
    }

    [TestMethod]
    public void ToDisplayName_AlreadyMixedCase_KeepsCase()
    {
        Assert.AreEqual("Station One", Station.ToDisplayName("Station One"));
    }

    [TestMethod]
    [DataRow("BELLAMBI_AWS", "Bellambi AWS")]
    [DataRow("ATLANTA_PEACHTREE_AP", "Atlanta Peachtree AP")]
    [DataRow("NEW_YORK_JFK_INTL_AP", "New York JFK Intl AP")]
    [DataRow("WILLIAMTOWN RAAF", "Williamtown RAAF")]
    [DataRow("SULTAN_MAHMUD_BADARUDDIN_II", "Sultan Mahmud Badaruddin II")]
    [DataRow("PORT MACQUARIE AIRPORT AWS (COMPARISON)", "Port Macquarie Airport AWS (Comparison)")]
    public void ToDisplayName_Abbreviation_KeepsCapitals(string name, string expected)
    {
        Assert.AreEqual(expected, Station.ToDisplayName(name));
    }

    [TestMethod]
    [DataRow("DALLAS_7_NE", "Dallas 7 NE")]
    [DataRow("TROY_3N", "Troy 3N")]
    [DataRow("BATTLE_CREEK_5NW", "Battle Creek 5NW")]
    public void ToDisplayName_CompassOffset_KeepsCapitals(string name, string expected)
    {
        Assert.AreEqual(expected, Station.ToDisplayName(name));
    }

    [TestMethod]
    [DataRow("MCKINLEY_PARK", "McKinley Park")]
    [DataRow("O'NEILL", "O'Neill")]
    [DataRow("M'SILA", "M'Sila")]
    [DataRow("KING'S_PARK", "King's Park")]
    [DataRow("CALGARY_INT'L_CS", "Calgary Int'l CS")]
    public void ToDisplayName_McOrApostrophe_CapitalisesAfterPrefix(string name, string expected)
    {
        Assert.AreEqual(expected, Station.ToDisplayName(name));
    }

    [TestMethod]
    [DataRow("MACAPA", "Macapa")]
    [DataRow("SAN PEDRO", "San Pedro")]
    public void ToDisplayName_OrdinaryWord_TitleCase(string name, string expected)
    {
        Assert.AreEqual(expected, Station.ToDisplayName(name));
    }

    private static Location CreateLocation(string name, string? stationName)
    {
        return new Location
        {
            Id = Guid.NewGuid(),
            Name = name,
            StationName = stationName,
            CountryCode = "AU",
            Country = "Australia",
            Coordinates = new Coordinates(-38, 144),
        };
    }
}
