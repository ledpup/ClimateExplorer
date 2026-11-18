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
