namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class RecentObservationsServiceTests
{
    private static readonly Guid LocationId = Guid.Parse("9f0ac026-4840-44df-8155-80c7f8d6a1d6");

    [TestMethod]
    public async Task GetPrecipitationRecordsIncludesPreviousMonthsAcrossCalendarYears()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(CreateSouthernHemisphereLocation(), previousMonthCount: 11, previousSeasonCount: 3);

        var previousMonths = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.PreviousMonth)
            .ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "Last month - May 2026",
                "April 2026",
                "March 2026",
                "February 2026",
                "January 2026",
                "December 2025",
                "November 2025",
                "October 2025",
                "September 2025",
                "August 2025",
                "July 2025",
            },
            previousMonths.Select(x => x.PeriodTitle).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(1, 11).ToArray(), previousMonths.Select(x => x.PeriodOffset!.Value).ToArray());
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsIncludesPreviousSeasonsAcrossCalendarYears()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(CreateSouthernHemisphereLocation(), previousMonthCount: 11, previousSeasonCount: 3);

        var previousSeasons = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.PreviousSeason)
            .ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "Autumn 2026",
                "Summer 2025-26",
                "Spring 2025",
            },
            previousSeasons.Select(x => x.PeriodTitle).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(1, 3).ToArray(), previousSeasons.Select(x => x.PeriodOffset!.Value).ToArray());
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsPreservesTileOrderAndKeysAcrossMultipleYears()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(CreateSouthernHemisphereLocation(), previousMonthCount: 11, previousSeasonCount: 3);
        var keys = result.Tiles.Select(x => $"{x.PeriodKind}:{x.PeriodTitle}").ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "Today",
                "Yesterday",
                "Last week",
                "June 2026 to date",
                "Last month - May 2026",
                "April 2026",
                "March 2026",
                "February 2026",
                "January 2026",
                "December 2025",
                "November 2025",
                "October 2025",
                "September 2025",
                "August 2025",
                "July 2025",
                "Winter to Date",
                "Autumn 2026",
                "Summer 2025-26",
                "Spring 2025",
                "2026 to date",
            },
            result.Tiles.Select(x => x.PeriodTitle).ToArray());
        Assert.AreEqual(keys.Count, keys.Distinct().Count());
    }

    [TestMethod]
    public void PeriodSelectionClampsCountsAndExposesDisabledStates()
    {
        var selection = new RecentObservationPeriodSelection();

        Assert.AreEqual(0, selection.PreviousMonthCount);
        Assert.AreEqual(0, selection.PreviousSeasonCount);
        Assert.IsFalse(selection.IsAddEarlierMonthDisabled);
        Assert.IsTrue(selection.IsRemoveMonthDisabled);
        Assert.IsFalse(selection.IsAddEarlierSeasonDisabled);
        Assert.IsTrue(selection.IsRemoveSeasonDisabled);

        for (var i = 0; i < 20; i++)
        {
            selection.AddEarlierMonth();
            selection.AddEarlierSeason();
        }

        Assert.AreEqual(11, selection.PreviousMonthCount);
        Assert.AreEqual(3, selection.PreviousSeasonCount);
        Assert.IsTrue(selection.IsAddEarlierMonthDisabled);
        Assert.IsFalse(selection.IsRemoveMonthDisabled);
        Assert.IsTrue(selection.IsAddEarlierSeasonDisabled);
        Assert.IsFalse(selection.IsRemoveSeasonDisabled);

        for (var i = 0; i < 20; i++)
        {
            selection.RemoveMonth();
            selection.RemoveSeason();
        }

        Assert.AreEqual(0, selection.PreviousMonthCount);
        Assert.AreEqual(0, selection.PreviousSeasonCount);
        Assert.IsFalse(selection.IsAddEarlierMonthDisabled);
        Assert.IsTrue(selection.IsRemoveMonthDisabled);
        Assert.IsFalse(selection.IsAddEarlierSeasonDisabled);
        Assert.IsTrue(selection.IsRemoveSeasonDisabled);
    }

    private static RecentObservationsService CreateService()
    {
        var dataService = new Mock<IDataService>();
        dataService
            .Setup(x => x.GetRecentObservations(LocationId, DataType.Precipitation, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Records = CreateDailyRecords(new DateOnly(2025, 7, 1), new DateOnly(2026, 6, 14)),
            });
        dataService
            .Setup(x => x.GetClimateRecords(
                It.IsAny<Guid>(),
                It.IsAny<DataType>(),
                It.IsAny<DataAdjustment?>(),
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<int?>()))
            .ReturnsAsync(new ClimateRecordsResponse());

        return new RecentObservationsService(
            dataService.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 6, 14, 12, 0, 0, TimeSpan.Zero)));
    }

    private static List<DataRecord> CreateDailyRecords(DateOnly startDate, DateOnly endDate)
    {
        var records = new List<DataRecord>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            records.Add(new DataRecord(date, 1d));
        }

        return records;
    }

    private static Location CreateSouthernHemisphereLocation()
    {
        return new Location
        {
            Id = LocationId,
            Name = "Canberra",
            CountryCode = "AU",
            Coordinates = new Coordinates(-35.3d, 149.1d),
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
