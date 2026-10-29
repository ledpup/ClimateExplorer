namespace ClimateExplorer.UnitTests.TestData;

using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.Stats.Model;

/// <summary>
/// Shared by the Core and Web test projects (linked into ClimateExplorer.Web.UnitTests), so the trend
/// assertions on both sides run against the same seeded series.
/// </summary>
internal static class TrendTestData
{
    internal static Location CreateLocation()
    {
        return new Location
        {
            Id = Guid.Empty,
            Name = "Unknown",
            Coordinates = new Coordinates(),
            CountryCode = "UU",
        };
    }

    internal static List<DataPoint> CreateNoisyLine(int startYear, int count)
    {
        var random = new Random(20261018);

        return [.. Enumerable.Range(0, count).Select(i => new DataPoint(startYear + i, 10 + (0.02 * i) + (random.NextDouble() - 0.5)))];
    }
}
