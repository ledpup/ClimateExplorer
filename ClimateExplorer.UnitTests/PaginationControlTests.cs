namespace ClimateExplorer.UnitTests;

using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Web.Client.Components.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class PaginationControlTests
{
    private const int Ellipsis = -1;

    [TestMethod]
    public void GetVisiblePages_TotalWithinMax_ReturnsAllPages()
    {
        var pages = PaginationControl.GetVisiblePages(totalPages: 9, currentPage: 5, maxPages: 9);

        CollectionAssert.AreEqual(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, pages);
    }

    [TestMethod]
    public void GetVisiblePages_NoPages_ReturnsEmpty()
    {
        var pages = PaginationControl.GetVisiblePages(totalPages: 0, currentPage: 1, maxPages: 15);

        Assert.IsEmpty(pages);
    }

    [TestMethod]
    public void GetVisiblePages_CurrentNearStart_HasTrailingEllipsisOnly()
    {
        var pages = PaginationControl.GetVisiblePages(totalPages: 50, currentPage: 2, maxPages: 9);

        CollectionAssert.AreEqual(new List<int> { 1, 2, 3, 4, 5, 6, 7, Ellipsis, 50 }, pages);
    }

    [TestMethod]
    public void GetVisiblePages_CurrentNearEnd_HasLeadingEllipsisOnly()
    {
        var pages = PaginationControl.GetVisiblePages(totalPages: 50, currentPage: 49, maxPages: 9);

        CollectionAssert.AreEqual(new List<int> { 1, Ellipsis, 44, 45, 46, 47, 48, 49, 50 }, pages);
    }

    [TestMethod]
    public void GetVisiblePages_CurrentInMiddle_HasBothEllipses()
    {
        var pages = PaginationControl.GetVisiblePages(totalPages: 50, currentPage: 25, maxPages: 9);

        CollectionAssert.AreEqual(new List<int> { 1, Ellipsis, 23, 24, 25, 26, 27, Ellipsis, 50 }, pages);
    }

    [TestMethod]
    public void GetVisiblePages_AnyInput_NeverExceedsMaxPages()
    {
        foreach (var maxPages in new[] { 9, 15 })
        {
            for (var totalPages = 1; totalPages <= 60; totalPages++)
            {
                for (var currentPage = 1; currentPage <= totalPages; currentPage++)
                {
                    var pages = PaginationControl.GetVisiblePages(totalPages, currentPage, maxPages);
                    var context = $"total {totalPages}, current {currentPage}, max {maxPages}";

                    Assert.IsLessThanOrEqualTo(maxPages, pages.Count, $"Too many slots: {context}");
                    Assert.AreEqual(1, pages[0], $"First page missing: {context}");
                    Assert.AreEqual(totalPages, pages[^1], $"Last page missing: {context}");
                    CollectionAssert.Contains(pages, currentPage, $"Current page missing: {context}");

                    var numbers = pages.Where(p => p != Ellipsis).ToList();
                    CollectionAssert.AreEqual(numbers.OrderBy(p => p).Distinct().ToList(), numbers, $"Pages not ascending and unique: {context}");
                }
            }
        }
    }
}
