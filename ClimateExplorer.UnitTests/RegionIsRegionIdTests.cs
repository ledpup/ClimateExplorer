namespace ClimateExplorer.UnitTests;

using System;
using ClimateExplorer.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers <see cref="Region.IsRegionId"/> - tells a "global" (region-based) chart series apart
/// from one tied to a real location.
/// </summary>
[TestClass]
public class RegionIsRegionIdTests
{
    [TestMethod]
    public void IsRegionId_KnownRegionId_ReturnsTrue()
    {
        Assert.IsTrue(Region.IsRegionId(Region.RegionId(Region.Atmosphere)));
    }

    [TestMethod]
    public void IsRegionId_RandomGuid_ReturnsFalse()
    {
        Assert.IsFalse(Region.IsRegionId(Guid.NewGuid()));
    }
}
