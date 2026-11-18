namespace ClimateExplorer.Core.UnitTests;

using System.Collections.Generic;
using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class StationAbbreviationsTests
{
    [TestMethod]
    public void Find_InitialismAndShortenedWords_ReturnsEachInOrder()
    {
        var found = StationAbbreviations.Find("Chicago Waukegan Rgnl AP");

        CollectionAssert.AreEqual(
            new List<(string, string)> { ("Rgnl", "Regional"), ("AP", "Airport") },
            found);
    }

    [TestMethod]
    public void Find_OtherCase_ReturnsUsualSpelling()
    {
        var found = StationAbbreviations.Find("Bellambi Aws");

        CollectionAssert.AreEqual(new List<(string, string)> { ("AWS", "Automatic weather station") }, found);
    }

    [TestMethod]
    public void Find_RepeatedAbbreviation_ReturnsItOnce()
    {
        var found = StationAbbreviations.Find("Kalamazoo Battle Ck Intl AP (Intl AP)");

        CollectionAssert.AreEqual(
            new List<(string, string)> { ("Intl", "International"), ("AP", "Airport") },
            found);
    }

    [TestMethod]
    public void Find_Apostrophe_MatchesWholeWord()
    {
        var found = StationAbbreviations.Find("Calgary Int'l CS");

        CollectionAssert.AreEqual(
            new List<(string, string)> { ("Int'l", "International"), ("CS", "Climate station") },
            found);
    }

    [TestMethod]
    [DataRow("Geelong (Avalon Airport)")]
    [DataRow("Laws Apex")]
    [DataRow("Base Belgrano II")]
    public void Find_NoAbbreviation_ReturnsEmpty(string name)
    {
        Assert.AreEqual(0, StationAbbreviations.Find(name).Count);
    }

    [TestMethod]
    [DataRow("AWS", true)]
    [DataRow("III", true)]
    [DataRow("INTL", false)]
    [DataRow("LAWS", false)]
    public void IsInitialism_Word_ReturnsWhetherKeptInCapitals(string word, bool expected)
    {
        Assert.AreEqual(expected, StationAbbreviations.IsInitialism(word));
    }
}
