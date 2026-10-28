namespace ClimateExplorer.UnitTests;

using System.Linq;
using ClimateExplorer.Web.Client.Components.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AutocompleteFilterTests
{
    private static readonly string[] Locations =
    [
        "North Sydney, Australia",
        "Sydney, Australia",
        "Melbourne, Australia",
        "Sydney Airport, Australia",
        "Hobart, Australia",
    ];

    [TestMethod]
    public void Filter_QueryShorterThanMinLength_ReturnsEmpty()
    {
        var results = AutocompleteFilter.Filter(Locations, x => x, "s", minLength: 2, max: 50, out var capped);

        Assert.IsEmpty(results);
        Assert.IsFalse(capped);
    }

    [TestMethod]
    public void Filter_MixedCaseQuery_MatchesCaseInsensitively()
    {
        var results = AutocompleteFilter.Filter(Locations, x => x, "mELb", minLength: 2, max: 50, out _);

        CollectionAssert.AreEqual(new[] { "Melbourne, Australia" }, results.ToArray());
    }

    [TestMethod]
    public void Filter_PrefixAndInnerMatches_ReturnsPrefixMatchesFirst()
    {
        var results = AutocompleteFilter.Filter(Locations, x => x, "syd", minLength: 2, max: 50, out _);

        CollectionAssert.AreEqual(
            new[] { "Sydney, Australia", "Sydney Airport, Australia", "North Sydney, Australia" },
            results.ToArray());
    }

    [TestMethod]
    public void Filter_MoreMatchesThanMax_ReturnsMaxAndSetsCapped()
    {
        var results = AutocompleteFilter.Filter(Locations, x => x, "australia", minLength: 2, max: 3, out var capped);

        CollectionAssert.AreEqual(
            new[] { "North Sydney, Australia", "Sydney, Australia", "Melbourne, Australia" },
            results.ToArray());
        Assert.IsTrue(capped);
    }

    [TestMethod]
    public void Filter_ExactlyMaxMatches_IsNotCapped()
    {
        var results = AutocompleteFilter.Filter(Locations, x => x, "syd", minLength: 2, max: 3, out var capped);

        Assert.HasCount(3, results);
        Assert.IsFalse(capped);
    }

    [TestMethod]
    public void Filter_QueryWithSurroundingWhitespace_IsTrimmed()
    {
        var results = AutocompleteFilter.Filter(Locations, x => x, "  hob  ", minLength: 2, max: 50, out _);

        CollectionAssert.AreEqual(new[] { "Hobart, Australia" }, results.ToArray());
    }

    [TestMethod]
    public void Filter_WhitespaceOnlyQuery_ReturnsEmpty()
    {
        var results = AutocompleteFilter.Filter(Locations, x => x, "   ", minLength: 0, max: 50, out var capped);

        Assert.IsEmpty(results);
        Assert.IsFalse(capped);
    }

    [TestMethod]
    public void Filter_NoMatches_ReturnsEmptyAndNotCapped()
    {
        var results = AutocompleteFilter.Filter(Locations, x => x, "perth", minLength: 2, max: 50, out var capped);

        Assert.IsEmpty(results);
        Assert.IsFalse(capped);
    }
}
