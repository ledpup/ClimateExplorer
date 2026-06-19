namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RecentObservationRetrievalMetadataSelectorTests
{
    [TestMethod]
    public void CollapsesEntriesSharingAUrlEvenWhenLabelsDiffer()
    {
        // Regression test: GHCNd downloads one CSV per station that backs both TempMax and
        // TempMin, so the temperature tab combines metadata from both requests. Even if their
        // labels differ, they must collapse to a single entry because the SourceUrl is the same.
        var maxMetadata = new RecentObservationSourceMetadata
        {
            SourceUrl = "https://www.ncei.noaa.gov/data/global-historical-climatology-network-daily/access/AE000041196.csv",
            SourceUrlLabel = "Station AE000041196, CSV",
            RetrievedAtUtc = new DateTimeOffset(2026, 6, 19, 3, 0, 0, TimeSpan.Zero),
        };
        var minMetadata = new RecentObservationSourceMetadata
        {
            SourceUrl = "https://www.ncei.noaa.gov/data/global-historical-climatology-network-daily/access/AE000041196.csv",
            SourceUrlLabel = "Different label, but same underlying file",
            RetrievedAtUtc = new DateTimeOffset(2026, 6, 19, 3, 1, 0, TimeSpan.Zero),
        };

        var result = RecentObservationRetrievalMetadataSelector.Select([maxMetadata, minMetadata]);

        Assert.HasCount(1, result);
        Assert.AreEqual(maxMetadata.SourceUrl, result[0].SourceUrl);
    }

    [TestMethod]
    public void KeepsSeparateEntriesForDistinctUrls()
    {
        // BOM downloads a separate ZIP per metric (max/min have different obs codes), so they
        // must remain as two distinct entries rather than being collapsed.
        var maxMetadata = new RecentObservationSourceMetadata
        {
            SourceUrl = "http://www.bom.gov.au/jsp/ncc/cdio/weatherData/av?p_nccObsCode=122",
            SourceUrlLabel = "TempMax station 086338, obs code 122, ZIP",
            RetrievedAtUtc = new DateTimeOffset(2026, 6, 19, 3, 0, 0, TimeSpan.Zero),
        };
        var minMetadata = new RecentObservationSourceMetadata
        {
            SourceUrl = "http://www.bom.gov.au/jsp/ncc/cdio/weatherData/av?p_nccObsCode=123",
            SourceUrlLabel = "TempMin station 086338, obs code 123, ZIP",
            RetrievedAtUtc = new DateTimeOffset(2026, 6, 19, 3, 0, 0, TimeSpan.Zero),
        };

        var result = RecentObservationRetrievalMetadataSelector.Select([maxMetadata, minMetadata]);

        Assert.HasCount(2, result);
    }

    [TestMethod]
    public void ExcludesEntriesMissingUrlOrRetrievalTimestamp()
    {
        var withoutUrl = new RecentObservationSourceMetadata
        {
            SourceUrl = null,
            RetrievedAtUtc = new DateTimeOffset(2026, 6, 19, 3, 0, 0, TimeSpan.Zero),
        };
        var withoutRetrievedAtUtc = new RecentObservationSourceMetadata
        {
            SourceUrl = "https://example.com/data.csv",
            RetrievedAtUtc = null,
        };

        var result = RecentObservationRetrievalMetadataSelector.Select([withoutUrl, withoutRetrievedAtUtc]);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void PicksTheMostRecentlyRetrievedEntryWhenCollapsing()
    {
        var older = new RecentObservationSourceMetadata
        {
            SourceUrl = "https://example.com/station.csv",
            SourceUrlLabel = "older",
            RetrievedAtUtc = new DateTimeOffset(2026, 6, 18, 3, 0, 0, TimeSpan.Zero),
        };
        var newer = new RecentObservationSourceMetadata
        {
            SourceUrl = "https://example.com/station.csv",
            SourceUrlLabel = "newer",
            RetrievedAtUtc = new DateTimeOffset(2026, 6, 19, 3, 0, 0, TimeSpan.Zero),
        };

        var result = RecentObservationRetrievalMetadataSelector.Select([older, newer]);

        Assert.HasCount(1, result);
        Assert.AreEqual("newer", result[0].SourceUrlLabel);
    }
}
