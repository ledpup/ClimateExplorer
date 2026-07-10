namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
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

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.MaximumPreviousDayCount,
            previousMonthCount: 11,
            previousSeasonCount: 3);

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

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.MaximumPreviousDayCount,
            previousMonthCount: 11,
            previousSeasonCount: 3);

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
    public async Task GetPrecipitationRecordsIncludesPreviousYears()
    {
        var service = CreateService(recentStartDate: new DateOnly(2024, 1, 1));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.MaximumPreviousDayCount,
            previousMonthCount: 11,
            previousSeasonCount: 3,
            previousYearCount: 2);
        var previousYears = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.PreviousYear)
            .ToList();

        CollectionAssert.AreEqual(new[] { "2025", "2024" }, previousYears.Select(x => x.PeriodTitle).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(1, 2).ToArray(), previousYears.Select(x => x.PeriodOffset!.Value).ToArray());
        Assert.AreEqual(new DateOnly(2025, 1, 1), previousYears[0].PeriodStartDate);
        Assert.AreEqual(new DateOnly(2025, 12, 31), previousYears[0].PeriodEndDate);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsPreservesTileOrderAndKeysAcrossMultipleYears()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.MaximumPreviousDayCount,
            previousMonthCount: 11,
            previousSeasonCount: 3);
        var keys = result.Tiles.Select(x => $"{x.PeriodKind}:{x.PeriodStartDate:yyyy-MM-dd}:{x.PeriodEndDate:yyyy-MM-dd}:{x.PeriodTitle}").ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "Today",
                "Yesterday",
                "12 June",
                "11 June",
                "10 June",
                "9 June",
                "8 June",
                "Latest 7 days",
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
                "Autumn 2026",
                "Summer 2025-26",
                "Spring 2025",
                "2026 to date",
            },
            result.Tiles.Select(x => x.PeriodTitle).ToArray());
        Assert.AreEqual(keys.Count, keys.Distinct().Count());
    }

    [TestMethod]
    [DataRow(2026, 3, 14, -35.3d)]
    [DataRow(2026, 6, 14, -35.3d)]
    [DataRow(2026, 9, 14, -35.3d)]
    [DataRow(2026, 12, 14, -35.3d)]
    [DataRow(2026, 3, 14, 40.7d)]
    [DataRow(2026, 6, 14, 40.7d)]
    [DataRow(2026, 9, 14, 40.7d)]
    [DataRow(2026, 12, 14, 40.7d)]
    public async Task GetPrecipitationRecordsHidesSeasonToDateDuringFirstSeasonMonth(int year, int month, int day, double latitude)
    {
        var today = new DateOnly(year, month, day);
        var service = CreateService(recentStartDate: today.AddMonths(-4), recentEndDate: today, today: today);

        var result = await service.GetPrecipitationRecords(
            CreateLocation(latitude),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 3);

        Assert.IsFalse(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.CurrentSeason));
    }

    [TestMethod]
    [DataRow(2026, 1, 1, -35.3d, false, false, true)]
    [DataRow(2026, 1, 15, -35.3d, true, false, true)]
    [DataRow(2026, 5, 1, -35.3d, false, true, true)]
    [DataRow(2026, 5, 2, -35.3d, true, true, true)]
    [DataRow(2026, 6, 14, -35.3d, true, true, false)]
    [DataRow(2026, 7, 14, -35.3d, true, true, true)]
    public async Task GetPrecipitationRecords_ReferenceDateAtPeriodBoundaries_TogglesOnlyCurrentPeriodTiles(
        int year,
        int month,
        int day,
        double latitude,
        bool expectCurrentMonth,
        bool expectYearToDate,
        bool expectCurrentSeason)
    {
        var today = new DateOnly(year, month, day);
        var service = CreateService(recentStartDate: today.AddYears(-2), recentEndDate: today, today: today);

        var result = await service.GetPrecipitationRecords(
            CreateLocation(latitude),
            previousDayCount: 2,
            previousMonthCount: 1,
            previousSeasonCount: 1,
            previousYearCount: 1);

        Assert.AreEqual(expectCurrentMonth, result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.CurrentMonth));
        Assert.AreEqual(expectYearToDate, result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.YearToDate));
        Assert.AreEqual(expectCurrentSeason, result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.CurrentSeason));
        Assert.IsTrue(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays));
        Assert.IsTrue(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.PreviousMonth));
        Assert.IsTrue(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.PreviousSeason));
        Assert.IsTrue(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.PreviousYear));

        var expectedPeriodKinds = new List<RecentObservationPeriodKind>
        {
            RecentObservationPeriodKind.Daily,
            RecentObservationPeriodKind.Daily,
            RecentObservationPeriodKind.LatestSevenDays,
        };

        if (expectCurrentMonth)
        {
            expectedPeriodKinds.Add(RecentObservationPeriodKind.CurrentMonth);
        }

        expectedPeriodKinds.Add(RecentObservationPeriodKind.PreviousMonth);

        if (expectCurrentSeason)
        {
            expectedPeriodKinds.Add(RecentObservationPeriodKind.CurrentSeason);
        }

        expectedPeriodKinds.Add(RecentObservationPeriodKind.PreviousSeason);

        if (expectYearToDate)
        {
            expectedPeriodKinds.Add(RecentObservationPeriodKind.YearToDate);
        }

        expectedPeriodKinds.Add(RecentObservationPeriodKind.PreviousYear);

        CollectionAssert.AreEqual(expectedPeriodKinds, result.Tiles.Select(x => x.PeriodKind).ToList());
    }

    [TestMethod]
    [DataRow(2026, 7, 14, -35.3d, "Winter to Date")]
    [DataRow(2026, 8, 14, -35.3d, "Winter to Date")]
    [DataRow(2026, 10, 14, -35.3d, "Spring to Date")]
    [DataRow(2026, 11, 14, -35.3d, "Spring to Date")]
    [DataRow(2027, 1, 14, -35.3d, "Summer to Date")]
    [DataRow(2027, 2, 14, -35.3d, "Summer to Date")]
    [DataRow(2026, 7, 14, 40.7d, "Summer to Date")]
    [DataRow(2026, 8, 14, 40.7d, "Summer to Date")]
    [DataRow(2026, 10, 14, 40.7d, "Autumn to Date")]
    [DataRow(2026, 11, 14, 40.7d, "Autumn to Date")]
    [DataRow(2027, 1, 14, 40.7d, "Winter to Date")]
    [DataRow(2027, 2, 14, 40.7d, "Winter to Date")]
    public async Task GetPrecipitationRecordsShowsSeasonToDateDuringSecondAndThirdSeasonMonths(
        int year,
        int month,
        int day,
        double latitude,
        string expectedTitle)
    {
        var today = new DateOnly(year, month, day);
        var service = CreateService(recentStartDate: today.AddMonths(-4), recentEndDate: today, today: today);

        var result = await service.GetPrecipitationRecords(
            CreateLocation(latitude),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 3);
        var currentSeason = result.Tiles.SingleOrDefault(x => x.PeriodKind == RecentObservationPeriodKind.CurrentSeason);

        Assert.IsNotNull(currentSeason);
        Assert.AreEqual(expectedTitle, currentSeason.PeriodTitle);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsShowsOneDailyTileByDefault()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.DefaultPreviousDayCount,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var dailyTiles = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.Daily)
            .ToList();

        Assert.HasCount(1, dailyTiles);
        Assert.AreEqual("Today", dailyTiles[0].PeriodTitle);
        Assert.AreEqual(new DateOnly(2026, 6, 14), dailyTiles[0].PeriodStartDate);
        Assert.AreEqual(1, dailyTiles[0].PeriodOffset);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsGeneratesDayTilesNewestToOldest()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 4,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var dailyTiles = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.Daily)
            .ToList();

        CollectionAssert.AreEqual(
            new[] { "Today", "Yesterday", "12 June", "11 June" },
            dailyTiles.Select(x => x.PeriodTitle).ToArray());
        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 6, 14), new DateOnly(2026, 6, 13), new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 11) },
            dailyTiles.Select(x => x.PeriodStartDate).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(1, 4).ToArray(), dailyTiles.Select(x => x.PeriodOffset!.Value).ToArray());
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsUsesDateLabelWhenLatestAvailableDayIsNotTodayOrYesterday()
    {
        var service = CreateService(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.DefaultPreviousDayCount,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var dailyTile = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);

        Assert.AreEqual("12 June", dailyTile.PeriodTitle);
        Assert.AreEqual(new DateOnly(2026, 6, 12), dailyTile.PeriodStartDate);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsLabelsLatestSevenDaysWithRecentDateRange()
    {
        var endLabel = new DateOnly(2026, 6, 14).ToString("d MMM", CultureInfo.CurrentCulture);
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual("Latest 7 days", latestSevenDays.PeriodTitle);
        Assert.AreEqual(new DateOnly(2026, 6, 8), latestSevenDays.PeriodStartDate);
        Assert.AreEqual(new DateOnly(2026, 6, 14), latestSevenDays.PeriodEndDate);
        Assert.AreEqual("7mm", latestSevenDays.PrimaryValue);
        Assert.IsNull(latestSevenDays.Note);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.AreEqual($"Wettest 7 days ending {endLabel}", latestSevenDays.HistoricalMaxLabel);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsLabelsDelayedLatestSevenDaysWithActualDateRange()
    {
        var latestDate = new DateOnly(2026, 6, 2);
        var latestSevenDaysStart = new DateOnly(2026, 5, 27);
        var endLabel = latestDate.ToString("d MMM", CultureInfo.CurrentCulture);
        var historicalRecords = CreateHistoricalRangeRecords(latestSevenDaysStart, latestDate);
        var service = CreateService(
            recentStartDate: new DateOnly(2026, 5, 1),
            recentEndDate: latestDate,
            historicalRecords: historicalRecords,
            today: new DateOnly(2026, 6, 14));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual("Latest 7 days", latestSevenDays.PeriodTitle);
        Assert.AreEqual(latestSevenDaysStart, latestSevenDays.PeriodStartDate);
        Assert.AreEqual(latestDate, latestSevenDays.PeriodEndDate);
        Assert.AreEqual("7mm", latestSevenDays.PrimaryValue);
        Assert.IsNull(latestSevenDays.Note);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.AreEqual($"Wettest 7 days ending {endLabel}", latestSevenDays.HistoricalMaxLabel);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsDefaultsReferenceDateToLatestAvailableObservation()
    {
        var today = new DateOnly(2026, 6, 14);
        var latestDate = today.AddDays(-1);
        var service = CreateService(
            recentStartDate: new DateOnly(2026, 6, 1),
            recentEndDate: latestDate,
            today: today);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var dailyTile = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(latestDate, result.ReferenceDate);
        Assert.AreEqual(new DateOnly(2026, 6, 1), result.MinimumReferenceDate);
        Assert.AreEqual(latestDate, result.MaximumReferenceDate);
        Assert.AreEqual("Yesterday", dailyTile.PeriodTitle);
        Assert.AreEqual(latestDate, latestSevenDays.PeriodEndDate);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsUsesReferenceDateWhenLatestDataIsTwoWeeksOld()
    {
        var today = new DateOnly(2026, 6, 14);
        var latestDate = today.AddDays(-14);
        var service = CreateService(
            recentStartDate: new DateOnly(2026, 5, 1),
            recentEndDate: latestDate,
            today: today);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var currentMonth = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.CurrentMonth);
        var yearToDate = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.YearToDate);

        Assert.AreEqual(latestDate, result.ReferenceDate);
        Assert.AreEqual(new DateOnly(2026, 5, 25), latestSevenDays.PeriodStartDate);
        Assert.AreEqual(latestDate, latestSevenDays.PeriodEndDate);
        Assert.AreEqual("May 2026", currentMonth.PeriodTitle);
        Assert.AreEqual(new DateOnly(2026, 5, 1), currentMonth.PeriodStartDate);
        Assert.AreEqual(latestDate, currentMonth.PeriodEndDate);
        Assert.AreEqual(new DateOnly(2026, 1, 1), yearToDate.PeriodStartDate);
        Assert.AreEqual(latestDate, yearToDate.PeriodEndDate);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsUsesClimateRecordsWhenLatestAvailableDataIsInPreviousYear()
    {
        var latestDate = new DateOnly(2024, 8, 15);
        var historicalRecords = CreateDailyRecords(new DateOnly(2024, 1, 1), latestDate);
        var service = CreateService(
            recentStartDate: new DateOnly(2026, 1, 1),
            recentEndDate: new DateOnly(2025, 12, 31),
            historicalRecords: historicalRecords,
            today: new DateOnly(2026, 6, 14));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 1,
            previousSeasonCount: 1);
        var dailyTile = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);
        var currentMonth = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.CurrentMonth);
        var previousMonth = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.PreviousMonth);

        Assert.AreEqual(latestDate, result.ReferenceDate);
        Assert.AreEqual("15 Aug 2024", dailyTile.PeriodTitle);
        Assert.AreEqual("August 2024 to date", currentMonth.PeriodTitle);
        Assert.AreEqual(new DateOnly(2024, 8, 1), currentMonth.PeriodStartDate);
        Assert.AreEqual(latestDate, currentMonth.PeriodEndDate);
        Assert.AreEqual("Last month - July 2024", previousMonth.PeriodTitle);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsShowsObservedValuesWithoutComparisonForOneYearOfData()
    {
        var service = CreateService(
            recentStartDate: new DateOnly(2025, 1, 1),
            recentEndDate: new DateOnly(2025, 12, 31),
            today: new DateOnly(2026, 6, 14));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(new DateOnly(2025, 12, 31), result.ReferenceDate);
        Assert.AreEqual("7mm", latestSevenDays.PrimaryValue);
        Assert.IsFalse(latestSevenDays.HasComparison);
        Assert.AreEqual(0, latestSevenDays.ComparablePeriodCount);
        Assert.IsFalse(latestSevenDays.CanShowHistoricalRecord);
        Assert.IsFalse(latestSevenDays.CanShowHistoricalRange);
        Assert.IsFalse(latestSevenDays.CanShowRank);
        Assert.IsFalse(latestSevenDays.CanShowPercentile);
        Assert.AreEqual("No comparable historical periods are available for this date range.", latestSevenDays.PercentileSentence);
    }

    [TestMethod]
    public async Task DailyPrecipitationTileShowsLimitedComparisonWithOneHistoricalYear()
    {
        var day = new DateOnly(2026, 6, 14);
        var dayLabel = day.ToString("d MMM", CultureInfo.CurrentCulture);
        var historicalRecords = CreateHistoricalSameDateRecords(day, startYear: 2025, endYear: 2025, valueOffset: 4d);
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var daily = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);
        var total = daily.MetricGroups.Single().Metrics.Single();

        Assert.AreEqual("1mm", daily.PrimaryValue);
        Assert.IsTrue(daily.HasComparison);
        Assert.AreEqual(1, daily.ComparablePeriodCount);
        Assert.IsTrue(daily.CanShowHistoricalRecord);
        Assert.IsFalse(daily.CanShowHistoricalRange);
        Assert.IsFalse(daily.CanShowRank);
        Assert.IsFalse(daily.CanShowPercentile);
        Assert.AreEqual($"Driest of 2 comparable {dayLabel} observations", daily.Headline);
        Assert.AreEqual("Ranking unavailable: only 1 comparable year.", daily.PercentileSentence);
        Assert.AreEqual("Limited history: comparison based on 1 comparable year.", daily.Note);
        Assert.IsNull(daily.HistoricalMaxLabel);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, total.RecordStatus);
        Assert.AreEqual("New low of 2", total.RecordStatusText);
        Assert.IsFalse(total.HasRecords);
        Assert.IsFalse(total.CanShowHistoricalRange);
    }

    [TestMethod]
    public async Task DailyTemperatureTileShowsHistoricalRangeWithTwoComparableYears()
    {
        var day = new DateOnly(2026, 6, 14);
        var dayLabel = day.ToString("d MMM", CultureInfo.CurrentCulture);
        var historicalRecords = CreateHistoricalSameDateRecords(day, startYear: 2024, endYear: 2025, valueOffset: 12d);
        var service = CreateTemperatureService(historicalRecords);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var daily = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);

        Assert.AreEqual("15.0°C", daily.PrimaryValue);
        Assert.IsTrue(daily.HasComparison);
        Assert.AreEqual(2, daily.ComparablePeriodCount);
        Assert.IsTrue(daily.CanShowHistoricalRecord);
        Assert.IsTrue(daily.CanShowHistoricalRange);
        Assert.IsFalse(daily.CanShowRank);
        Assert.IsFalse(daily.CanShowPercentile);
        Assert.AreEqual($"Warmest of 3 comparable {dayLabel} observations", daily.Headline);
        Assert.AreEqual("Ranking unavailable: only 2 comparable years.", daily.PercentileSentence);
        Assert.AreEqual("Limited history: comparison based on 2 comparable years.", daily.Note);
        Assert.AreEqual($"Warmest {dayLabel}", daily.HistoricalMaxLabel);
        Assert.AreEqual("14.0°C", daily.HistoricalMaxValue);
        Assert.AreEqual($"Coolest {dayLabel}", daily.HistoricalMinLabel);
        Assert.AreEqual("13.0°C", daily.HistoricalMinValue);
    }

    [TestMethod]
    public async Task AggregatePrecipitationTileShowsRangeWithTwoComparablePeriods()
    {
        var endLabel = new DateOnly(2026, 6, 14).ToString("d MMM", CultureInfo.CurrentCulture);
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14), startYear: 2024, endYear: 2025);
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var total = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.PeriodRecords).Metrics.Single();

        Assert.AreEqual("7mm", latestSevenDays.PrimaryValue);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.AreEqual(2, latestSevenDays.ComparablePeriodCount);
        Assert.IsTrue(latestSevenDays.CanShowHistoricalRecord);
        Assert.IsTrue(latestSevenDays.CanShowHistoricalRange);
        Assert.IsFalse(latestSevenDays.CanShowRank);
        Assert.IsFalse(latestSevenDays.CanShowPercentile);
        Assert.AreEqual("Driest of 3 comparable periods", latestSevenDays.Headline);
        Assert.AreEqual("Ranking unavailable: only 2 comparable periods.", latestSevenDays.PercentileSentence);
        Assert.AreEqual("Limited history: comparison based on 2 comparable periods.", latestSevenDays.Note);
        Assert.AreEqual($"Wettest 7 days ending {endLabel}", latestSevenDays.HistoricalMaxLabel);
        Assert.AreEqual("84mm", latestSevenDays.HistoricalMaxValue);
        Assert.AreEqual($"Driest 7 days ending {endLabel}", latestSevenDays.HistoricalMinLabel);
        Assert.AreEqual("77mm", latestSevenDays.HistoricalMinValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, total.RecordStatus);
        Assert.AreEqual("New low of 3", total.RecordStatusText);
        Assert.AreEqual("84mm", total.RecordHigh!.Value);
        Assert.AreEqual("2025", total.RecordHigh.Year);
        Assert.AreEqual("77mm", total.RecordLow!.Value);
        Assert.AreEqual("2024", total.RecordLow.Year);
    }

    [TestMethod]
    public async Task PrecipitationTilesShowWettestAndDriestHistoricalRangesForEveryPeriodKind()
    {
        var today = new DateOnly(2026, 7, 14);
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 1, 1), today, startYear: 2024, endYear: 2025);
        var service = CreateService(
            recentEndDate: today,
            historicalRecords: historicalRecords,
            today: today);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var tiles = new[]
        {
            result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily),
            result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays),
            result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.CurrentMonth),
            result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.CurrentSeason),
            result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.YearToDate),
        };

        foreach (var tile in tiles)
        {
            AssertPrecipitationHistoricalRange(tile);
        }
    }

    [TestMethod]
    public async Task AggregatePrecipitationTileSuppressesRankBelowMinimumRankSampleSize()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14), startYear: 2017, endYear: 2025);
        var service = CreateService(historicalRecords: historicalRecords, recentValue: _ => 15d);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var total = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.PeriodRecords).Metrics.Single();

        Assert.AreEqual("105mm", latestSevenDays.PrimaryValue);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.AreEqual(9, latestSevenDays.ComparablePeriodCount);
        Assert.IsTrue(latestSevenDays.CanShowHistoricalRecord);
        Assert.IsTrue(latestSevenDays.CanShowHistoricalRange);
        Assert.IsFalse(latestSevenDays.CanShowRank);
        Assert.IsFalse(latestSevenDays.CanShowPercentile);
        Assert.AreEqual("Limited historical comparison", latestSevenDays.Headline);
        Assert.AreEqual("Ranking unavailable: only 9 comparable periods.", latestSevenDays.PercentileSentence);
        Assert.IsNull(total.RankText);
        Assert.AreEqual(RecentObservationRecordStatus.None, total.RecordStatus);
        Assert.IsTrue(total.HasRecords);
    }

    [TestMethod]
    public async Task AggregatePrecipitationTileShowsRankAtMinimumRankSampleSize()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14), startYear: 2016, endYear: 2025);
        var service = CreateService(historicalRecords: historicalRecords, recentValue: _ => 15d);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var total = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.PeriodRecords).Metrics.Single();

        Assert.AreEqual("105mm", latestSevenDays.PrimaryValue);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.AreEqual(10, latestSevenDays.ComparablePeriodCount);
        Assert.IsTrue(latestSevenDays.CanShowHistoricalRecord);
        Assert.IsTrue(latestSevenDays.CanShowHistoricalRange);
        Assert.IsTrue(latestSevenDays.CanShowRank);
        Assert.IsTrue(latestSevenDays.CanShowPercentile);
        Assert.AreEqual("Near average rainfall", latestSevenDays.Headline);
        Assert.AreEqual("Wetter than 36% of comparable periods", latestSevenDays.PercentileSentence);
        Assert.AreEqual("5th lowest of 11", total.RankText);
    }

    [TestMethod]
    public async Task UpToViewDateModeShowsLimitedHistoryComparison()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14), startYear: 2023, endYear: 2025);
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0,
            referenceDate: new DateOnly(2026, 6, 14),
            comparisonEndMode: ComparisonEndMode.ReferenceDate);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(ComparisonEndMode.ReferenceDate, result.ComparisonEndMode);
        Assert.AreEqual("7mm", latestSevenDays.PrimaryValue);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.AreEqual(3, latestSevenDays.ComparablePeriodCount);
        Assert.IsTrue(latestSevenDays.CanShowHistoricalRecord);
        Assert.IsTrue(latestSevenDays.CanShowHistoricalRange);
        Assert.IsFalse(latestSevenDays.CanShowRank);
        Assert.IsFalse(latestSevenDays.CanShowPercentile);
        Assert.AreEqual("Driest of 4 comparable periods", latestSevenDays.Headline);
        Assert.AreEqual("Ranking unavailable: only 3 comparable periods.", latestSevenDays.PercentileSentence);
        Assert.AreEqual("Limited history: comparison based on 3 comparable periods.", latestSevenDays.Note);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsGeneratesMeaningfulTilesForSixMonthsOfData()
    {
        var service = CreateService(
            recentStartDate: new DateOnly(2024, 1, 1),
            recentEndDate: new DateOnly(2024, 6, 30),
            today: new DateOnly(2026, 6, 14));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.MaximumPreviousDayCount,
            previousMonthCount: 11,
            previousSeasonCount: 3);
        var previousMonths = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.PreviousMonth)
            .Select(x => x.PeriodTitle)
            .ToArray();

        Assert.AreEqual(new DateOnly(2024, 6, 30), result.ReferenceDate);
        CollectionAssert.AreEqual(
            new[] { "Last month - May 2024", "April 2024", "March 2024", "February 2024", "January 2024" },
            previousMonths);
        Assert.IsTrue(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.YearToDate));
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsSnapsRequestedReferenceDateToLatestAvailableObservationOnOrBeforeSelection()
    {
        var requestedReferenceDate = new DateOnly(2026, 4, 17);
        var resolvedReferenceDate = new DateOnly(2026, 4, 15);
        var service = CreateService(
            recentStartDate: new DateOnly(2026, 1, 1),
            recentEndDate: new DateOnly(2026, 6, 14),
            includeRecentRecord: date => date != new DateOnly(2026, 4, 16) && date != requestedReferenceDate);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 2,
            previousMonthCount: 0,
            previousSeasonCount: 0,
            referenceDate: requestedReferenceDate);
        var dailyTiles = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.Daily)
            .ToList();
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var currentMonth = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.CurrentMonth);
        var yearToDate = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.YearToDate);

        Assert.AreEqual(requestedReferenceDate, result.RequestedReferenceDate);
        Assert.AreEqual(resolvedReferenceDate, result.ReferenceDate);
        Assert.AreEqual("No observation is available for 17 Apr 2026; showing 15 Apr 2026 instead.", result.ReferenceDateNote);
        CollectionAssert.AreEqual(
            new[] { resolvedReferenceDate, new DateOnly(2026, 4, 14) },
            dailyTiles.Select(x => x.PeriodStartDate).ToArray());
        Assert.AreEqual(new DateOnly(2026, 4, 9), latestSevenDays.PeriodStartDate);
        Assert.AreEqual(resolvedReferenceDate, latestSevenDays.PeriodEndDate);
        Assert.AreEqual(new DateOnly(2026, 4, 1), currentMonth.PeriodStartDate);
        Assert.AreEqual(resolvedReferenceDate, currentMonth.PeriodEndDate);
        Assert.AreEqual(new DateOnly(2026, 1, 1), yearToDate.PeriodStartDate);
        Assert.AreEqual(resolvedReferenceDate, yearToDate.PeriodEndDate);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsDeduplicatesOverlappingRecentAndClimateRecordsByDate()
    {
        var historicalRecords = CreateDailyRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14), _ => 1d);
        var service = CreateService(
            recentStartDate: new DateOnly(2026, 6, 10),
            recentEndDate: new DateOnly(2026, 6, 14),
            historicalRecords: historicalRecords,
            recentValue: _ => 10d);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual("52mm", latestSevenDays.PrimaryValue);
        Assert.AreEqual(7, latestSevenDays.AvailableObservationCount);
        Assert.AreEqual(7, latestSevenDays.ExpectedObservationCount);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsDefaultsComparisonEndModeToFullDataset()
    {
        var referenceDate = new DateOnly(1980, 6, 15);
        var historicalRecords = CreateHistoricalComparisonModeRecords(referenceDate.AddDays(-6), referenceDate);
        var service = CreateService(
            recentStartDate: new DateOnly(2026, 1, 1),
            recentEndDate: new DateOnly(2025, 12, 31),
            historicalRecords: historicalRecords,
            today: new DateOnly(2026, 6, 14));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0,
            referenceDate: referenceDate);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(ComparisonEndMode.FullDataset, result.ComparisonEndMode);
        Assert.AreEqual(new DateOnly(1980, 6, 9), latestSevenDays.PeriodStartDate);
        Assert.AreEqual(referenceDate, latestSevenDays.PeriodEndDate);
        Assert.AreEqual("70mm", latestSevenDays.PrimaryValue);
        Assert.AreEqual("700mm", latestSevenDays.HistoricalMaxValue);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsComparisonEndModeReferenceDateUsesOnlyEarlierRecords()
    {
        var referenceDate = new DateOnly(1980, 6, 15);
        var historicalRecords = CreateHistoricalComparisonModeRecords(referenceDate.AddDays(-6), referenceDate);
        var service = CreateService(
            recentStartDate: new DateOnly(2026, 1, 1),
            recentEndDate: new DateOnly(2025, 12, 31),
            historicalRecords: historicalRecords,
            today: new DateOnly(2026, 6, 14));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0,
            referenceDate: referenceDate,
            comparisonEndMode: ComparisonEndMode.ReferenceDate);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(ComparisonEndMode.ReferenceDate, result.ComparisonEndMode);
        Assert.AreEqual(new DateOnly(1980, 6, 9), latestSevenDays.PeriodStartDate);
        Assert.AreEqual(referenceDate, latestSevenDays.PeriodEndDate);
        Assert.AreEqual("70mm", latestSevenDays.PrimaryValue);
        Assert.AreEqual("7mm", latestSevenDays.HistoricalMaxValue);
    }

    [TestMethod]
    public async Task CalculatePrecipitationRecordsWithDifferentOptionsDoesNotFetchAgain()
    {
        var location = CreateSouthernHemisphereLocation();
        var dataService = new Mock<IDataService>();
        SetupEmptyClimateRecords(dataService);
        dataService
            .Setup(x => x.GetRecentObservations(LocationId, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Precipitation = new RecentObservationSeries
                {
                    Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14)),
                },
            });
        var service = CreateRecentObservationsService(dataService);

        var dataSet = await service.LoadPrecipitationData(location);

        _ = service.Calculate(
            location,
            dataSet,
            new RecentObservationsOptions
            {
                ReferenceDate = new DateOnly(2026, 6, 14),
                ComparisonEndMode = ComparisonEndMode.FullDataset,
                PreviousDayCount = 1,
                PreviousMonthCount = 0,
                PreviousSeasonCount = 0,
            });
        var recalculated = service.Calculate(
            location,
            dataSet,
            new RecentObservationsOptions
            {
                ReferenceDate = new DateOnly(2026, 6, 7),
                ComparisonEndMode = ComparisonEndMode.ReferenceDate,
                PreviousDayCount = 2,
                PreviousMonthCount = 0,
                PreviousSeasonCount = 0,
            });

        Assert.AreEqual(new DateOnly(2026, 6, 7), recalculated.ReferenceDate);
        dataService.Verify(x => x.GetRecentObservations(LocationId, false), Times.Once);
        dataService.Verify(
            x => x.GetClimateRecords(
                LocationId,
                DataType.Precipitation,
                null,
                false,
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()),
            Times.Once);
    }

    [TestMethod]
    public async Task CalculatePrecipitationRecordsPreservesSourceMetadataAcrossDifferentOptions()
    {
        var sourceMetadata = new RecentObservationSourceMetadata
        {
            SourceCode = "GHCNd",
            SourceName = "Global Historical Climatology Network Daily",
            StationId = "ASN00070014",
            SourceUrl = "https://www.ncei.noaa.gov/data/global-historical-climatology-network-daily/access/ASN00070014.csv",
            SourceUrlLabel = "Precipitation station ASN00070014, CSV",
            RetrievedAtUtc = new DateTimeOffset(2026, 6, 19, 3, 42, 0, TimeSpan.Zero),
        };
        var location = CreateSouthernHemisphereLocation();
        var dataService = new Mock<IDataService>();
        SetupEmptyClimateRecords(dataService);
        dataService
            .Setup(x => x.GetRecentObservations(LocationId, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Precipitation = new RecentObservationSeries
                {
                    Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14)),
                    SourceMetadata = sourceMetadata,
                },
            });
        var service = CreateRecentObservationsService(dataService);

        var dataSet = await service.LoadPrecipitationData(location);
        var result = service.Calculate(
            location,
            dataSet,
            new RecentObservationsOptions
            {
                ReferenceDate = new DateOnly(2026, 6, 14),
                ComparisonEndMode = ComparisonEndMode.FullDataset,
                PreviousDayCount = 1,
                PreviousMonthCount = 0,
                PreviousSeasonCount = 0,
            });
        var recalculated = service.Calculate(
            location,
            dataSet,
            new RecentObservationsOptions
            {
                ReferenceDate = new DateOnly(2026, 6, 7),
                ComparisonEndMode = ComparisonEndMode.ReferenceDate,
                PreviousDayCount = 2,
                PreviousMonthCount = 0,
                PreviousSeasonCount = 0,
            });
        var thresholded = recalculated.ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default);

        Assert.AreEqual(sourceMetadata, result.SourceMetadata.Single());
        Assert.AreEqual(sourceMetadata, recalculated.SourceMetadata.Single());
        Assert.AreEqual(sourceMetadata, thresholded.SourceMetadata.Single());
        Assert.AreEqual(new DateOnly(2026, 6, 14), result.ReferenceDate);
        Assert.AreEqual(ComparisonEndMode.FullDataset, result.ComparisonEndMode);
        Assert.AreEqual(new DateOnly(2026, 6, 7), recalculated.ReferenceDate);
        Assert.AreEqual(ComparisonEndMode.ReferenceDate, recalculated.ComparisonEndMode);
    }

    [TestMethod]
    public async Task LoadPrecipitationDataCachesByLocation()
    {
        var otherLocationId = Guid.Parse("46b814ed-6e56-47e2-92ca-097526b84d4d");
        var dataService = new Mock<IDataService>();
        SetupEmptyClimateRecords(dataService);
        dataService
            .Setup(x => x.GetRecentObservations(It.IsAny<Guid>(), false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Precipitation = new RecentObservationSeries
                {
                    Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14)),
                },
            });
        var service = CreateRecentObservationsService(dataService);

        await service.LoadPrecipitationData(CreateSouthernHemisphereLocation());
        await service.LoadPrecipitationData(CreateSouthernHemisphereLocation());
        await service.LoadPrecipitationData(CreateSouthernHemisphereLocation(otherLocationId));

        dataService.Verify(x => x.GetRecentObservations(LocationId, false), Times.Once);
        dataService.Verify(x => x.GetRecentObservations(otherLocationId, false), Times.Once);
        dataService.Verify(
            x => x.GetClimateRecords(
                LocationId,
                DataType.Precipitation,
                null,
                false,
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()),
            Times.Once);
        dataService.Verify(
            x => x.GetClimateRecords(
                otherLocationId,
                DataType.Precipitation,
                null,
                false,
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()),
            Times.Once);
    }

    [TestMethod]
    public async Task GetPrecipitationRecordsComparesGeneratedDaysAgainstHistoricalSameCalendarDate()
    {
        var dayLabel = new DateOnly(2026, 6, 13).ToString("d MMM", CultureInfo.CurrentCulture);
        var historicalRecords = CreateHistoricalSameDateRecords(new DateOnly(2026, 6, 13), startYear: 2000, endYear: 2025, valueOffset: 0d);
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 2,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var generatedDay = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.Daily)
            .Single(x => x.PeriodStartDate == new DateOnly(2026, 6, 13));

        Assert.IsTrue(generatedDay.HasComparison);
        Assert.AreEqual($"Wettest {dayLabel}", generatedDay.HistoricalMaxLabel);
        Assert.AreEqual("26mm", generatedDay.HistoricalMaxValue);
        Assert.AreEqual($"Driest {dayLabel}", generatedDay.HistoricalMinLabel);
        Assert.AreEqual("1mm", generatedDay.HistoricalMinValue);
    }

    [TestMethod]
    public async Task GetTemperatureRecordsGeneratedDaysKeepTemperatureStatsAndHistoricalRange()
    {
        var dayLabel = new DateOnly(2026, 6, 13).ToString("d MMM", CultureInfo.CurrentCulture);
        var historicalRecords = CreateHistoricalSameDateRecords(new DateOnly(2026, 6, 13), startYear: 2000, endYear: 2025, valueOffset: 10d);
        var service = CreateTemperatureService(historicalRecords);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 2,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var generatedDay = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.Daily)
            .Single(x => x.PeriodStartDate == new DateOnly(2026, 6, 13));

        Assert.IsTrue(generatedDay.HasComparison);
        Assert.AreEqual("Mean temperature", generatedDay.PrimaryLabel);
        CollectionAssert.AreEqual(new[] { "Historical average", "Anomaly" }, generatedDay.Stats.Select(x => x.Label).ToArray());
        CollectionAssert.AreEqual(new[] { "Max temp", "Min temp" }, generatedDay.SupportingStats.Select(x => x.Label).ToArray());
        Assert.AreEqual($"Warmest {dayLabel}", generatedDay.HistoricalMaxLabel);
        Assert.AreEqual("36.0°C", generatedDay.HistoricalMaxValue);
        Assert.AreEqual($"Coolest {dayLabel}", generatedDay.HistoricalMinLabel);
        Assert.AreEqual("11.0°C", generatedDay.HistoricalMinValue);
    }

    [TestMethod]
    public async Task CompletenessThresholdAllowsComparisonsAtFullCompleteness()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(1f, latestSevenDays.Completeness);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.IsNull(latestSevenDays.Note);
        Assert.IsNotNull(latestSevenDays.HistoricalMaxLabel);
    }

    [TestMethod]
    public async Task CompletenessThresholdAllowsComparisonsExactlyAtThreshold()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var baseLatestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var latestSevenDays = baseLatestSevenDays.ApplyCompletenessThreshold(baseLatestSevenDays.Completeness);

        Assert.AreEqual(5, latestSevenDays.AvailableObservationCount);
        Assert.AreEqual(7, latestSevenDays.ExpectedObservationCount);
        Assert.AreEqual(5f / 7f, latestSevenDays.Completeness);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.IsNull(latestSevenDays.Note);
        Assert.IsNotNull(latestSevenDays.HistoricalMaxLabel);
    }

    [TestMethod]
    public async Task CompletenessThresholdSuppressesComparisonsBelowThreshold()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.IsFalse(latestSevenDays.HasComparison);
        Assert.AreEqual("Comparison unavailable", latestSevenDays.Headline);
        Assert.AreEqual("Only 5 of 7 days are available (only 71% completeness).", latestSevenDays.Note);
        Assert.IsNull(latestSevenDays.HistoricalMaxLabel);
        CollectionAssert.AreEqual(new[] { "Historical average", "Anomaly" }, latestSevenDays.Stats.Select(x => x.Label).ToArray());
    }

    [TestMethod]
    public async Task CompletenessThresholdSuppressesVariationBelowThreshold()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var variation = (RecentObservationVariationTabViewModel)latestSevenDays.AvailableExpandedTabs.Single(x => x.Key == MetricGroupKey.Variation);
        var precipitation = variation.Metrics.Single();

        Assert.IsNull(precipitation.HistoricalRangeText);
        Assert.IsNull(precipitation.TypicalVariationText);
        Assert.IsNull(precipitation.StandardScoreLabel);
        Assert.IsNull(precipitation.StandardScoreValue);
        Assert.AreEqual("Recent observations are below the completeness threshold.", precipitation.UnavailableReason);
    }

    [TestMethod]
    public async Task CompletenessThresholdUsesMissingMinAndMaxTemperatureObservations()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14), valueOffset: 10d);
        var service = CreateTemperatureService(
            historicalRecords,
            includeMaxRecord: date => date != new DateOnly(2026, 6, 9),
            includeMinRecord: date => date != new DateOnly(2026, 6, 12));

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(5, latestSevenDays.AvailableObservationCount);
        Assert.AreEqual(7, latestSevenDays.ExpectedObservationCount);
        Assert.IsFalse(latestSevenDays.HasComparison);
        Assert.AreEqual("Only 5 of 7 days are available (only 71% completeness).", latestSevenDays.Note);
        CollectionAssert.AreEqual(
            new[] { "Historical average", "Anomaly" },
            latestSevenDays.Stats.Select(x => x.Label).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Average max temp", "Average min temp" },
            latestSevenDays.SupportingStats.Select(x => x.Label).ToArray());
    }

    [TestMethod]
    public async Task CompletenessThresholdSuppressesSupportingStatRecordsBelowThreshold()
    {
        // Max/min temp are individually new records on the days that are present, but
        // completeness for the period is below threshold (5 of 7 days), so the tile
        // must not claim a new record for any metric, including the supporting stats.
        var historicalMean = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14), valueOffset: 10d);
        var historicalMax = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var historicalMin = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateTemperatureService(
            historicalMean,
            includeMaxRecord: date => date != new DateOnly(2026, 6, 9),
            includeMinRecord: date => date != new DateOnly(2026, 6, 12),
            historicalMaxRecords: historicalMax,
            historicalMinRecords: historicalMin,
            recentMax: _ => 100d,
            recentMin: _ => -50d);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var baseLatestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        // Sanity check: without completeness suppression, max and min would both show
        // as new records (this is the bug as originally reported).
        Assert.IsTrue(baseLatestSevenDays.HasComparison);
        Assert.IsNotEmpty(baseLatestSevenDays.SupportingStats);
        Assert.IsTrue(baseLatestSevenDays.SupportingStats.All(x => x.RecordStatus == RecentObservationRecordStatus.NewRecord));

        var suppressed = baseLatestSevenDays.ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default);

        Assert.IsFalse(suppressed.HasComparison);
        Assert.IsTrue(suppressed.SupportingStats.All(x => x.RecordStatus == RecentObservationRecordStatus.None));
        Assert.IsTrue(suppressed.SupportingStats.All(x => x.RecordStatusText is null));
        Assert.IsTrue(suppressed.MetricGroups
            .SelectMany(group => group.Metrics)
            .All(metric => metric.RecordStatus == RecentObservationRecordStatus.None && metric.RecordStatusText is null));
    }

    [TestMethod]
    public async Task CompletenessThresholdPreservesSupportingStatRecordsAboveThreshold()
    {
        // Same record-breaking max/min values as the suppression test above, but with
        // full completeness, so the new-record lozenges must still show correctly.
        var historicalMean = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14), valueOffset: 10d);
        var historicalMax = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var historicalMin = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateTemperatureService(
            historicalMean,
            historicalMaxRecords: historicalMax,
            historicalMinRecords: historicalMin,
            recentMax: _ => 100d,
            recentMin: _ => -50d);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(1f, latestSevenDays.Completeness);
        Assert.IsTrue(latestSevenDays.HasComparison);
        Assert.IsNotEmpty(latestSevenDays.SupportingStats);
        Assert.IsTrue(latestSevenDays.SupportingStats.All(x => x.RecordStatus == RecentObservationRecordStatus.NewRecord));
        Assert.IsTrue(latestSevenDays.SupportingStats.All(x => x.RecordStatusText == "NEW RECORD"));
        Assert.IsTrue(latestSevenDays.MetricGroups
            .Single(group => group.Key == MetricGroupKey.PeriodRecords)
            .Metrics
            .Where(metric => metric.Label != "Mean temperature")
            .All(metric => metric.RecordStatus == RecentObservationRecordStatus.NewRecord));
    }

    [TestMethod]
    public void ApplyCompletenessThresholdSuppressesRecordStatusOnEveryMetricBelowThreshold()
    {
        var tile = CreateTileWithRecordStatuses(availableObservationCount: 5, expectedObservationCount: 7);

        var suppressed = tile.ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default);

        Assert.IsFalse(suppressed.HasComparison);
        Assert.AreEqual(RecentObservationRecordStatus.None, suppressed.PrimaryRecordStatus);
        Assert.IsNull(suppressed.PrimaryRecordStatusText);
        Assert.IsTrue(suppressed.Stats.All(x => x.RecordStatus == RecentObservationRecordStatus.None && x.RecordStatusText is null));
        Assert.IsTrue(suppressed.SupportingStats.All(x => x.RecordStatus == RecentObservationRecordStatus.None && x.RecordStatusText is null));
        Assert.IsTrue(suppressed.MetricGroups
            .SelectMany(group => group.Metrics)
            .All(metric =>
                metric.RecordStatus == RecentObservationRecordStatus.None &&
                metric.RecordStatusText is null &&
                metric.RankText is null &&
                metric.RecordHigh is null &&
                metric.RecordLow is null));
    }

    [TestMethod]
    public void ApplyCompletenessThresholdPreservesRecordStatusOnEveryMetricAboveThreshold()
    {
        var tile = CreateTileWithRecordStatuses(availableObservationCount: 7, expectedObservationCount: 7);

        var allowed = tile.ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default);

        Assert.IsTrue(allowed.HasComparison);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, allowed.PrimaryRecordStatus);
        Assert.AreEqual("New record", allowed.PrimaryRecordStatusText);
        Assert.IsTrue(allowed.Stats.All(x => x.RecordStatus == RecentObservationRecordStatus.NewRecord));
        Assert.IsTrue(allowed.SupportingStats.All(x => x.RecordStatus is RecentObservationRecordStatus.NewRecord or RecentObservationRecordStatus.EqualRecord));
        Assert.IsTrue(allowed.MetricGroups
            .SelectMany(group => group.Metrics)
            .All(metric => metric.RecordStatus is RecentObservationRecordStatus.NewRecord or RecentObservationRecordStatus.EqualRecord));
    }

    private static RecentObservationTileViewModel CreateTileWithRecordStatuses(int availableObservationCount, int expectedObservationCount)
    {
        return new RecentObservationTileViewModel
        {
            PeriodKind = RecentObservationPeriodKind.LatestSevenDays,
            PeriodTitle = "Last 7 days",
            HasComparison = true,
            PrimaryLabel = "Mean temperature",
            PrimaryValue = "20.0°C",
            PrimaryRecordStatus = RecentObservationRecordStatus.NewRecord,
            PrimaryRecordStatusText = "New record",
            Stats =
            [
                new RecentObservationStatViewModel
                {
                    Label = "Historical average",
                    Value = "15.0°C",
                    RecordStatus = RecentObservationRecordStatus.NewRecord,
                    RecordStatusText = "New record",
                },
            ],
            SupportingStats =
            [
                new RecentObservationStatViewModel
                {
                    Label = "Max temp",
                    Value = "30.0°C",
                    RecordStatus = RecentObservationRecordStatus.NewRecord,
                    RecordStatusText = "New record",
                },
                new RecentObservationStatViewModel
                {
                    Label = "Min temp",
                    Value = "5.0°C",
                    RecordStatus = RecentObservationRecordStatus.EqualRecord,
                    RecordStatusText = "Equal record",
                },
            ],
            MetricGroups =
            [
                new RecentObservationMetricGroupViewModel
                {
                    Key = MetricGroupKey.PeriodRecords,
                    Title = "Period records",
                    Metrics =
                    [
                        new RecentObservationRecordsViewModel
                        {
                            Label = "Average maximum temperature",
                            CurrentValue = "30.0°C",
                            RecordStatus = RecentObservationRecordStatus.NewRecord,
                            RecordStatusText = "New record",
                            RecordHigh = new RecentObservationMetricRecordViewModel { Label = "Record high", Value = "25.0°C", Year = "2025" },
                        },
                    ],
                },
                new RecentObservationMetricGroupViewModel
                {
                    Key = MetricGroupKey.DayRecords,
                    Title = "Daily extremes",
                    Metrics =
                    [
                        new RecentObservationRecordsViewModel
                        {
                            Label = "Highest daily maximum",
                            CurrentValue = "35.0°C",
                            RecordStatus = RecentObservationRecordStatus.EqualRecord,
                            RecordStatusText = "Equal record",
                        },
                    ],
                },
            ],
            AvailableObservationCount = availableObservationCount,
            ExpectedObservationCount = expectedObservationCount,
        };
    }

    [TestMethod]
    public async Task CompletenessThresholdUsesMissingPrecipitationObservations()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        Assert.AreEqual(5, latestSevenDays.AvailableObservationCount);
        Assert.AreEqual(7, latestSevenDays.ExpectedObservationCount);
        Assert.IsFalse(latestSevenDays.HasComparison);
        Assert.AreEqual("Only 5 of 7 days are available (only 71% completeness).", latestSevenDays.Note);
    }

    [TestMethod]
    public void CompletenessThresholdConvertsUiPercentagesToInternalThresholds()
    {
        Assert.AreEqual(0.8f, RecentObservationCompletenessThreshold.FromPercentage(80));
        Assert.AreEqual(80, RecentObservationCompletenessThreshold.ToPercentage(RecentObservationCompletenessThreshold.Default));
        Assert.AreEqual(0f, RecentObservationCompletenessThreshold.FromPercentage(-10));
        Assert.AreEqual(1f, RecentObservationCompletenessThreshold.FromPercentage(120));
    }

    [TestMethod]
    public async Task CompletenessThresholdVisibilityTogglesWithoutReloadingTiles()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var baseLatestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var suppressed = baseLatestSevenDays.ApplyCompletenessThreshold(0.80f);
        var allowed = baseLatestSevenDays.ApplyCompletenessThreshold(0.70f);

        Assert.IsFalse(suppressed.HasComparison);
        Assert.IsTrue(allowed.HasComparison);
        Assert.IsNull(allowed.Note);
        Assert.IsNotNull(allowed.HistoricalMaxLabel);
    }

    [TestMethod]
    public async Task CompletenessThresholdAppliesToAllRangeTiles()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var baseRangeTiles = result.Tiles
            .Where(x => x.PeriodKind is RecentObservationPeriodKind.LatestSevenDays or
                RecentObservationPeriodKind.CurrentMonth or
                RecentObservationPeriodKind.CurrentSeason or
                RecentObservationPeriodKind.YearToDate)
            .ToList();
        var thresholdedRangeTiles = result
            .ApplyCompletenessThreshold(1f)
            .Tiles
            .Where(x => x.PeriodKind is RecentObservationPeriodKind.LatestSevenDays or
                RecentObservationPeriodKind.CurrentMonth or
                RecentObservationPeriodKind.CurrentSeason or
                RecentObservationPeriodKind.YearToDate)
            .ToList();

        Assert.HasCount(3, baseRangeTiles);
        Assert.IsTrue(baseRangeTiles.All(x => x.HasComparison));
        Assert.IsTrue(thresholdedRangeTiles.All(x => !x.HasComparison));
        Assert.IsTrue(thresholdedRangeTiles.All(x => !string.IsNullOrWhiteSpace(x.Note)));
    }

    [TestMethod]
    public async Task PeriodSelectionCreatesAddButtonLabelsFromGeneratedTiles()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service, previousYearCount: RecentObservationPeriodSelection.MaximumPreviousYearCount);
        var selection = new RecentObservationPeriodSelection();

        Assert.IsFalse(tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.CurrentSeason));
        Assert.AreEqual("Add Yesterday", selection.CreateAddButtonLabel(RecentObservationPeriodKind.Daily, tiles, "day"));
        Assert.AreEqual("Add May 2026", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousMonth, tiles, "month"));
        Assert.AreEqual("Add Autumn 2026", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));
        Assert.AreEqual("Add 2025", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousYear, tiles, "year"));
    }

    [TestMethod]
    public async Task PeriodSelectionAddYearLabelMovesToNextPreviousYear()
    {
        var service = CreateService(recentStartDate: new DateOnly(2024, 1, 1));
        var tiles = await GetGeneratedTiles(service, previousYearCount: RecentObservationPeriodSelection.MaximumPreviousYearCount);
        var selection = new RecentObservationPeriodSelection();
        var yearOffsets = GetAvailableOffsets(tiles, RecentObservationPeriodKind.PreviousYear);

        Assert.AreEqual("Add 2025", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousYear, tiles, "year"));

        selection.AddEarlierYear(yearOffsets);

        Assert.AreEqual("Add 2024", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousYear, tiles, "year"));
    }

    [TestMethod]
    public async Task PeriodSelectionAddSeasonLabelStartsWithMostRecentlyCompletedSeasonWhenSeasonToDateIsHidden()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();
        var seasonOffsets = GetAvailableOffsets(tiles, RecentObservationPeriodKind.PreviousSeason);

        Assert.IsFalse(tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.CurrentSeason));
        Assert.AreEqual("Add Autumn 2026", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));

        selection.AddEarlierSeason(seasonOffsets);

        Assert.AreEqual("Add Summer 2025-26", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));

        selection.AddEarlierSeason(seasonOffsets);

        Assert.AreEqual("Add Spring 2025", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));
    }

    [TestMethod]
    public async Task PeriodSelectionAddButtonLabelsCrossCalendarYears()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();
        var monthOffsets = GetAvailableOffsets(tiles, RecentObservationPeriodKind.PreviousMonth);

        for (var i = 0; i < 5; i++)
        {
            selection.AddEarlierMonth(monthOffsets);
        }

        Assert.AreEqual("Add December 2025", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousMonth, tiles, "month"));
    }

    [TestMethod]
    public async Task PeriodSelectionAddButtonLabelsCrossSeasonYearBoundaries()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();
        var seasonOffsets = GetAvailableOffsets(tiles, RecentObservationPeriodKind.PreviousSeason);

        Assert.AreEqual("Add Autumn 2026", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));

        selection.AddEarlierSeason(seasonOffsets);

        Assert.AreEqual("Add Summer 2025-26", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));

        selection.AddEarlierSeason(seasonOffsets);

        Assert.AreEqual("Add Spring 2025", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));
    }

    [TestMethod]
    public async Task PeriodSelectionAddButtonLabelUpdatesAfterRemovalAndAddition()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();
        var dayOffsets = GetAvailableOffsets(tiles, RecentObservationPeriodKind.Daily);

        selection.AddEarlierDay(dayOffsets);
        selection.AddEarlierDay(dayOffsets);

        Assert.AreEqual("Add 11 June", selection.CreateAddButtonLabel(RecentObservationPeriodKind.Daily, tiles, "day"));

        selection.Remove(GetTile(tiles, RecentObservationPeriodKind.Daily, 3));

        Assert.AreEqual("Add 12 June", selection.CreateAddButtonLabel(RecentObservationPeriodKind.Daily, tiles, "day"));

        selection.AddEarlierDay(dayOffsets);

        Assert.AreEqual("Add 11 June", selection.CreateAddButtonLabel(RecentObservationPeriodKind.Daily, tiles, "day"));
    }

    [TestMethod]
    public async Task PeriodSelectionAddButtonLabelRetainsLastValidLabelAtMaximum()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();
        var dayOffsets = GetAvailableOffsets(tiles, RecentObservationPeriodKind.Daily);

        for (var i = 0; i < 20; i++)
        {
            selection.AddEarlierDay(dayOffsets);
        }

        Assert.IsFalse(selection.CanAddEarlierDay(dayOffsets));
        Assert.AreEqual("Add 8 June", selection.CreateAddButtonLabel(RecentObservationPeriodKind.Daily, tiles, "day"));
    }

    [TestMethod]
    public void PeriodSelectionMarksOnlyDynamicTilesRemovable()
    {
        var selection = new RecentObservationPeriodSelection();
        var currentDay = CreateTile(RecentObservationPeriodKind.Daily, 1, "Today");
        var previousDay = CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday");
        var latestSevenDays = CreateTile(RecentObservationPeriodKind.LatestSevenDays, null, "Latest 7 days");
        var currentMonth = CreateTile(RecentObservationPeriodKind.CurrentMonth, null, "June 2026 to date");
        var previousMonth = CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026");
        var currentSeason = CreateTile(RecentObservationPeriodKind.CurrentSeason, null, "Winter to Date");
        var previousSeason = CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026");
        var yearToDate = CreateTile(RecentObservationPeriodKind.YearToDate, null, "2026 to date");
        var previousYear = CreateTile(RecentObservationPeriodKind.PreviousYear, 1, "2025");

        selection.AddEarlierDay();
        selection.AddEarlierMonth();
        selection.AddEarlierSeason();
        selection.AddEarlierYear();

        Assert.IsFalse(selection.IsRemovable(currentDay));
        Assert.IsTrue(selection.IsRemovable(previousDay));
        Assert.IsFalse(selection.IsRemovable(latestSevenDays));
        Assert.IsFalse(selection.IsRemovable(currentMonth));
        Assert.IsTrue(selection.IsRemovable(previousMonth));
        Assert.IsFalse(selection.IsRemovable(currentSeason));
        Assert.IsTrue(selection.IsRemovable(previousSeason));
        Assert.IsFalse(selection.IsRemovable(yearToDate));
        Assert.IsTrue(selection.IsRemovable(previousYear));
    }

    [TestMethod]
    public void PeriodSelectionRemovesSpecificDynamicTiles()
    {
        var selection = new RecentObservationPeriodSelection();
        var previousDay = CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday");
        var previousMonth = CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026");
        var previousSeason = CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026");
        var previousYear = CreateTile(RecentObservationPeriodKind.PreviousYear, 1, "2025");

        selection.AddEarlierDay();
        selection.AddEarlierDay();
        selection.AddEarlierMonth();
        selection.AddEarlierMonth();
        selection.AddEarlierSeason();
        selection.AddEarlierYear();

        selection.Remove(previousDay);
        selection.Remove(previousMonth);
        selection.Remove(previousSeason);
        selection.Remove(previousYear);

        Assert.IsFalse(selection.IsVisible(previousDay));
        Assert.IsTrue(selection.IsVisible(CreateTile(RecentObservationPeriodKind.Daily, 3, "12 June")));
        Assert.IsFalse(selection.IsVisible(previousMonth));
        Assert.IsTrue(selection.IsVisible(CreateTile(RecentObservationPeriodKind.PreviousMonth, 2, "April 2026")));
        Assert.IsFalse(selection.IsVisible(previousSeason));
        Assert.IsFalse(selection.IsVisible(previousYear));
    }

    [TestMethod]
    public void PeriodSelectionAddAfterRemoveContinuesWithNextEarlierAvailablePeriod()
    {
        var selection = new RecentObservationPeriodSelection();

        selection.AddEarlierDay();
        selection.AddEarlierDay();
        selection.Remove(CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday"));
        selection.AddEarlierDay();

        selection.AddEarlierMonth();
        selection.AddEarlierMonth();
        selection.Remove(CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026"));
        selection.AddEarlierMonth();

        selection.AddEarlierSeason();
        selection.AddEarlierSeason();
        selection.Remove(CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026"));
        selection.AddEarlierSeason();

        selection.AddEarlierYear();
        selection.AddEarlierYear();
        selection.Remove(CreateTile(RecentObservationPeriodKind.PreviousYear, 1, "2025"));
        selection.AddEarlierYear();

        CollectionAssert.AreEqual(
            new[] { "Today", "12 June", "11 June" },
            CreateOrderedDynamicTiles(RecentObservationPeriodKind.Daily, 4)
                .Where(selection.IsVisible)
                .Select(x => x.PeriodTitle)
                .ToArray());
        CollectionAssert.AreEqual(
            new[] { "PreviousMonth 2", "PreviousMonth 3" },
            CreateOrderedDynamicTiles(RecentObservationPeriodKind.PreviousMonth, 3)
                .Where(selection.IsVisible)
                .Select(x => x.PeriodTitle)
                .ToArray());
        CollectionAssert.AreEqual(
            new[] { "PreviousSeason 2", "PreviousSeason 3" },
            CreateOrderedDynamicTiles(RecentObservationPeriodKind.PreviousSeason, 3)
                .Where(selection.IsVisible)
                .Select(x => x.PeriodTitle)
                .ToArray());
        CollectionAssert.AreEqual(
            new[] { "PreviousYear 2", "PreviousYear 3" },
            CreateOrderedDynamicTiles(RecentObservationPeriodKind.PreviousYear, 3)
                .Where(selection.IsVisible)
                .Select(x => x.PeriodTitle)
                .ToArray());
    }

    [TestMethod]
    public void PeriodSelectionRespectsMaxAddLimits()
    {
        var selection = new RecentObservationPeriodSelection();

        for (var i = 0; i < 20; i++)
        {
            selection.AddEarlierDay();
            selection.AddEarlierMonth();
            selection.AddEarlierSeason();
            selection.AddEarlierYear();
        }

        Assert.AreEqual(RecentObservationPeriodSelection.MaximumPreviousDayCount, selection.PreviousDayCount);
        Assert.AreEqual(RecentObservationPeriodSelection.MaximumPreviousMonthCount, selection.PreviousMonthCount);
        Assert.AreEqual(RecentObservationPeriodSelection.MaximumPreviousSeasonCount, selection.PreviousSeasonCount);
        Assert.AreEqual(RecentObservationPeriodSelection.MaximumPreviousYearCount, selection.PreviousYearCount);
        Assert.IsTrue(selection.IsAddEarlierDayDisabled);
        Assert.IsTrue(selection.IsAddEarlierMonthDisabled);
        Assert.IsTrue(selection.IsAddEarlierSeasonDisabled);
        Assert.IsTrue(selection.IsAddEarlierYearDisabled);
        Assert.IsFalse(selection.CanAddEarlierDay(Enumerable.Range(1, 20)));
        Assert.IsFalse(selection.CanAddEarlierMonth(Enumerable.Range(1, 20)));
        Assert.IsFalse(selection.CanAddEarlierSeason(Enumerable.Range(1, 20)));
        Assert.IsFalse(selection.CanAddEarlierYear(Enumerable.Range(1, 20)));
    }

    [TestMethod]
    public void PeriodSelectionKeepsFilteredTileOrder()
    {
        var selection = new RecentObservationPeriodSelection();
        var tiles = new[]
        {
            CreateTile(RecentObservationPeriodKind.Daily, 1, "Today"),
            CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday"),
            CreateTile(RecentObservationPeriodKind.Daily, 3, "12 June"),
            CreateTile(RecentObservationPeriodKind.LatestSevenDays, null, "Latest 7 days"),
            CreateTile(RecentObservationPeriodKind.CurrentMonth, null, "June 2026 to date"),
            CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026"),
            CreateTile(RecentObservationPeriodKind.PreviousMonth, 2, "April 2026"),
            CreateTile(RecentObservationPeriodKind.CurrentSeason, null, "Winter to Date"),
            CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026"),
            CreateTile(RecentObservationPeriodKind.YearToDate, null, "2026 to date"),
            CreateTile(RecentObservationPeriodKind.PreviousYear, 1, "2025"),
        };

        selection.AddEarlierDay();
        selection.AddEarlierMonth();
        selection.AddEarlierMonth();
        selection.Remove(CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026"));
        selection.AddEarlierSeason();
        selection.AddEarlierYear();

        CollectionAssert.AreEqual(
            new[]
            {
                "Today",
                "Yesterday",
                "Latest 7 days",
                "June 2026 to date",
                "April 2026",
                "Winter to Date",
                "Autumn 2026",
                "2026 to date",
                "2025",
            },
            tiles.Where(selection.IsVisible).Select(x => x.PeriodTitle).ToArray());
    }

    [TestMethod]
    public void PeriodSelectionDefaultsToOneCurrentDayAndNoDynamicMonthsOrSeasons()
    {
        var selection = new RecentObservationPeriodSelection();

        Assert.AreEqual(1, selection.PreviousDayCount);
        Assert.AreEqual(0, selection.PreviousMonthCount);
        Assert.AreEqual(0, selection.PreviousSeasonCount);
        Assert.AreEqual(0, selection.PreviousYearCount);
        Assert.IsFalse(selection.IsAddEarlierDayDisabled);
        Assert.IsFalse(selection.IsAddEarlierMonthDisabled);
        Assert.IsFalse(selection.IsAddEarlierSeasonDisabled);
        Assert.IsFalse(selection.IsAddEarlierYearDisabled);
        Assert.IsTrue(selection.IsVisible(CreateTile(RecentObservationPeriodKind.Daily, 1, "Today")));
        Assert.IsFalse(selection.IsVisible(CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday")));
        Assert.IsFalse(selection.IsVisible(CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026")));
        Assert.IsFalse(selection.IsVisible(CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026")));
        Assert.IsFalse(selection.IsVisible(CreateTile(RecentObservationPeriodKind.PreviousYear, 1, "2025")));
    }

    [TestMethod]
    public async Task ExpandedTilesExposePeriodAndDailyExtremesMetricGroups()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        CollectionAssert.AreEqual(new[] { MetricGroupKey.PeriodRecords, MetricGroupKey.DayRecords }, latestSevenDays.MetricGroups.Select(x => x.Key).ToArray());
        CollectionAssert.AreEqual(new[] { "Period records", "Day records" }, latestSevenDays.MetricGroups.Select(x => x.Title).ToArray());

        var period = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.PeriodRecords);
        CollectionAssert.AreEqual(new[] { "Total precipitation" }, period.Metrics.Select(x => x.Label).ToArray());
        Assert.AreEqual("7mm", period.Metrics[0].CurrentValue);

        var dailyExtremes = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.DayRecords);
        CollectionAssert.AreEqual(new[] { "Highest daily precipitation" }, dailyExtremes.Metrics.Select(x => x.Label).ToArray());
        Assert.AreEqual("1mm", dailyExtremes.Metrics[0].CurrentValue);
    }

    [TestMethod]
    public async Task ExpandedTilesExposePeriodDayRecordsAndVariationTabs()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        CollectionAssert.AreEqual(
            new[] { MetricGroupKey.PeriodRecords, MetricGroupKey.DayRecords, MetricGroupKey.Variation },
            latestSevenDays.AvailableExpandedTabs.Select(x => x.Key).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Period records", "Day records", "Variation" },
            latestSevenDays.AvailableExpandedTabs.Select(x => x.Title).ToArray());
    }

    [TestMethod]
    public async Task ExpandedTilesUseConcisePeriodMetricGroupLabels()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 6,
            previousSeasonCount: 2);

        AssertMetricGroupLabel(result, RecentObservationPeriodKind.LatestSevenDays, null, "Period records");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.CurrentMonth, null, "Period records");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.PreviousMonth, 1, "Period records");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.PreviousMonth, 6, "Period records");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.PreviousSeason, 1, "Period records");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.PreviousSeason, 2, "Period records");
        AssertMetricGroupLabel(result, RecentObservationPeriodKind.YearToDate, null, "Period records");
    }

    [TestMethod]
    public async Task DailyTilesExposeRecordsAndVariationExpandedTabs()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 3,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var dailyTiles = result.Tiles
            .Where(x => x.PeriodKind == RecentObservationPeriodKind.Daily)
            .ToList();

        Assert.HasCount(3, dailyTiles);
        Assert.IsTrue(dailyTiles.All(x => x.MetricGroups.Count == 1));
        Assert.IsTrue(dailyTiles.All(x => x.MetricGroups[0].Key == MetricGroupKey.Day));
        Assert.IsTrue(dailyTiles.All(x => x.AvailableExpandedTabs.Count == 2));
        Assert.IsTrue(dailyTiles.All(x => x.AvailableExpandedTabs[0].Key == MetricGroupKey.Day));
        Assert.IsTrue(dailyTiles.All(x => x.AvailableExpandedTabs[0].Title == "Records"));
        Assert.IsTrue(dailyTiles.All(x => x.AvailableExpandedTabs[1].Key == MetricGroupKey.Variation));
        Assert.IsTrue(dailyTiles.All(x => x.AvailableExpandedTabs[1].Title == "Variation"));
    }

    [TestMethod]
    public async Task ExpandedTilesOmitRecordsWhenNoHistoryIsAvailable()
    {
        var service = CreateService();

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var dailyTile = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);
        var dayGroup = dailyTile.MetricGroups.Single();
        Assert.AreEqual(MetricGroupKey.Day, dayGroup.Key);
        var total = dayGroup.Metrics.Single();

        Assert.AreEqual("Precipitation", total.Label);
        Assert.AreEqual("1mm", total.CurrentValue);
        Assert.IsFalse(total.HasRecords);
        Assert.IsNull(total.RecordHigh);
        Assert.IsNull(total.RecordLow);
        Assert.IsNull(total.RankText);
        Assert.AreEqual(RecentObservationRecordStatus.None, total.RecordStatus);
    }

    [TestMethod]
    public async Task PrecipitationDailyExtremesDetectHistoricalRecords()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        // Recent 7-day total is 7mm, below every historical equivalent period
        // (min 77mm in 2000), so it is a new low record (driest).
        var total = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.PeriodRecords).Metrics.Single();
        Assert.AreEqual("7mm", total.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, total.RecordStatus);
        Assert.IsNull(total.RankText);
        Assert.AreEqual("252mm", total.RecordHigh!.Value);
        Assert.AreEqual("2025", total.RecordHigh.Year);
        Assert.AreEqual("77mm", total.RecordLow!.Value);
        Assert.AreEqual("2000", total.RecordLow.Year);

        var highestDaily = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.DayRecords).Metrics.Single();
        Assert.AreEqual("1mm", highestDaily.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, highestDaily.RecordStatus);
        Assert.AreEqual("39mm", highestDaily.RecordHigh!.Value);
        Assert.AreEqual("2025", highestDaily.RecordHigh.Year);
        Assert.AreEqual("14mm", highestDaily.RecordLow!.Value);
        Assert.AreEqual("2000", highestDaily.RecordLow.Year);
    }

    [TestMethod]
    public async Task GetPrecipitationRecords_VariationTab_ExposesHistoricalRangeTypicalVariationAndScore()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var variation = (RecentObservationVariationTabViewModel)latestSevenDays.AvailableExpandedTabs.Single(x => x.Key == MetricGroupKey.Variation);
        var precipitation = variation.Metrics.Single();

        Assert.AreEqual("Precipitation", precipitation.Label);
        Assert.AreEqual("Historical range: 77mm to 252mm", precipitation.HistoricalRangeText);
        Assert.AreEqual("Typical variation: ±52.5mm", precipitation.TypicalVariationText);
        Assert.IsTrue(precipitation.CurrentPeriodText!.StartsWith("Latest 7 days: ", StringComparison.Ordinal));
        Assert.AreEqual("standard score", precipitation.StandardScoreLabel);
        Assert.AreEqual("-3.0×", precipitation.StandardScoreValue);
        Assert.AreEqual(26, precipitation.ComparablePeriodCount);
    }

    [TestMethod]
    public async Task GetPrecipitationRecords_DailyVariationTab_UsesDailyObservationMetric()
    {
        var historicalRecords = CreateHistoricalSameDateRecords(new DateOnly(2026, 6, 14), startYear: 2000, endYear: 2025, valueOffset: 0d);
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var daily = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);
        var variation = (RecentObservationVariationTabViewModel)daily.AvailableExpandedTabs.Single(x => x.Key == MetricGroupKey.Variation);
        var precipitation = variation.Metrics.Single();

        Assert.AreEqual("Precipitation", precipitation.Label);
        Assert.AreEqual("Historical range: 1mm to 26mm", precipitation.HistoricalRangeText);
        Assert.AreEqual("Typical variation: ±7.5mm", precipitation.TypicalVariationText);
        Assert.IsTrue(precipitation.CurrentPeriodText!.StartsWith("14 June 2026: ", StringComparison.Ordinal));
        Assert.AreEqual("standard score", precipitation.StandardScoreLabel);
        Assert.AreEqual("-1.7×", precipitation.StandardScoreValue);
    }

    [TestMethod]
    public async Task GetPrecipitationRecords_CurrentSeasonVariationTab_UsesSeasonToDateCurrentPeriodLabel()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 15));
        var service = CreateService(recentEndDate: new DateOnly(2026, 7, 15), historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var currentSeason = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.CurrentSeason);
        var variation = (RecentObservationVariationTabViewModel)currentSeason.AvailableExpandedTabs.Single(x => x.Key == MetricGroupKey.Variation);
        var precipitation = variation.Metrics.Single();

        Assert.IsTrue(precipitation.CurrentPeriodText!.StartsWith("Winter 2026 to date: ", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task GetPrecipitationRecords_ZeroVariationHistory_OmitsStandardScore()
    {
        var historicalRecords = CreateHistoricalDailyValues(
            new DateOnly(2026, 6, 8),
            new DateOnly(2026, 6, 14),
            (_, _) => 2d);
        var service = CreateService(historicalRecords: historicalRecords);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var variation = (RecentObservationVariationTabViewModel)latestSevenDays.AvailableExpandedTabs.Single(x => x.Key == MetricGroupKey.Variation);
        var precipitation = variation.Metrics.Single();

        Assert.AreEqual("Historical range: 14mm to 14mm", precipitation.HistoricalRangeText);
        Assert.AreEqual("Typical variation: ±0mm", precipitation.TypicalVariationText);
        Assert.IsNull(precipitation.StandardScoreLabel);
        Assert.IsNull(precipitation.StandardScoreValue);
    }

    [TestMethod]
    public async Task TemperatureMetricGroupsComputeCurrentValuesAndRecords()
    {
        var historicalMax = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (_, date) => date.Day);
        var historicalMin = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (_, date) => date.Day - 3);
        var service = CreateTemperatureServiceWithExtremes(
            recentMax: date => date.Day + 20,
            recentMin: date => date.Day,
            historicalMax,
            historicalMin);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);

        var period = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.PeriodRecords);
        CollectionAssert.AreEqual(
            new[] { "Average maximum temperature", "Average minimum temperature", "Mean temperature" },
            period.Metrics.Select(x => x.Label).ToArray());

        var avgMax = period.Metrics.Single(x => x.Label == "Average maximum temperature");
        Assert.AreEqual("31.0°C", avgMax.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, avgMax.RecordStatus);
        Assert.AreEqual("New record", avgMax.RecordStatusText);
        Assert.IsNull(avgMax.RankText);
        Assert.AreEqual("11.0°C", avgMax.RecordHigh!.Value);
        Assert.AreEqual("2000", avgMax.RecordHigh.Year);

        var dailyExtremes = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.DayRecords);
        CollectionAssert.AreEqual(
            new[] { "Highest daily maximum", "Lowest daily maximum", "Highest daily minimum", "Lowest daily minimum" },
            dailyExtremes.Metrics.Select(x => x.Label).ToArray());

        var highestDailyMax = dailyExtremes.Metrics.Single(x => x.Label == "Highest daily maximum");
        Assert.AreEqual("34.0°C", highestDailyMax.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, highestDailyMax.RecordStatus);
        Assert.AreEqual("14.0°C", highestDailyMax.RecordHigh!.Value);

        // The period's coolest daily maximum (28°C) is warmer than any historical
        // coolest daily maximum (8°C) — a record at the high end, the very case the
        // old single-direction "102nd lowest of 102" wording got wrong.
        var lowestDailyMax = dailyExtremes.Metrics.Single(x => x.Label == "Lowest daily maximum");
        Assert.AreEqual("28.0°C", lowestDailyMax.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, lowestDailyMax.RecordStatus);
        Assert.IsNull(lowestDailyMax.RankText);
        Assert.AreEqual("8.0°C", lowestDailyMax.RecordHigh!.Value);

        var lowestDailyMin = dailyExtremes.Metrics.Single(x => x.Label == "Lowest daily minimum");
        Assert.AreEqual("8.0°C", lowestDailyMin.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, lowestDailyMin.RecordStatus);
        Assert.AreEqual("5.0°C", lowestDailyMin.RecordHigh!.Value);
    }

    [TestMethod]
    public async Task GetTemperatureRecords_VariationTabs_ExposeExpectedTemperatureMetrics()
    {
        var historicalMax = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (year, _) => year - 2000);
        var historicalMin = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (year, _) => year - 1990);
        var service = CreateTemperatureServiceWithExtremes(
            recentMax: _ => 50d,
            recentMin: _ => 10d,
            historicalMax,
            historicalMin);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var latestSevenDaysVariation = (RecentObservationVariationTabViewModel)latestSevenDays.AvailableExpandedTabs.Single(x => x.Key == MetricGroupKey.Variation);

        CollectionAssert.AreEqual(
            new[] { "Average max temp", "Average min temp", "Mean temperature" },
            latestSevenDaysVariation.Metrics.Select(x => x.Label).ToArray());
        Assert.AreEqual("Historical range: 0.0°C to 25.0°C", latestSevenDaysVariation.Metrics[0].HistoricalRangeText);
        Assert.AreEqual("Typical variation: ±7.5°C", latestSevenDaysVariation.Metrics[0].TypicalVariationText);
        Assert.AreEqual("standard score", latestSevenDaysVariation.Metrics[0].StandardScoreLabel);
        Assert.AreEqual("+5.0×", latestSevenDaysVariation.Metrics[0].StandardScoreValue);

        var daily = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);
        var dailyVariation = (RecentObservationVariationTabViewModel)daily.AvailableExpandedTabs.Single(x => x.Key == MetricGroupKey.Variation);

        CollectionAssert.AreEqual(
            new[] { "Maximum", "Minimum", "Mean" },
            dailyVariation.Metrics.Select(x => x.Label).ToArray());
    }

    [TestMethod]
    public async Task GetTemperatureRecords_DayRecordsMetricsHaveOccurrences_ExposesCurrentAndHistoricalDates()
    {
        var historicalMax = CreateHistoricalDailyValues(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 14),
            (year, date) => (year, date.Day) switch
            {
                (2020, 10) => 60d,
                (2000, _) => 10d,
                _ => 20d + ((year - 2000) * 0.1d) + ((date.Day - 8) * 0.01d),
            });
        var historicalMin = CreateHistoricalDailyValues(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 14),
            (year, date) => (year, date.Day) switch
            {
                (2021, 9) => 35d,
                (2002, 11) => -10d,
                (2000, _) => 5d,
                _ => 15d + ((year - 2000) * 0.05d) + ((date.Day - 8) * 0.01d),
            });
        var service = CreateTemperatureServiceWithExtremes(
            recentMax: date => date.Day switch
            {
                9 => 12.3d,
                12 => 42.6d,
                _ => 25d,
            },
            recentMin: date => date.Day switch
            {
                10 => 21.4d,
                11 => 2.5d,
                _ => 10d,
            },
            historicalMax,
            historicalMin);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var dailyExtremes = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.DayRecords);

        var highestDailyMax = dailyExtremes.Metrics.Single(x => x.Label == "Highest daily maximum");
        Assert.AreEqual("42.6°C", highestDailyMax.CurrentValue);
        Assert.AreEqual(new DateOnly(2026, 6, 12), highestDailyMax.CurrentValueDate);
        Assert.AreEqual(new DateOnly(2020, 6, 10), highestDailyMax.RecordHigh!.Date);
        Assert.AreEqual(new DateOnly(2000, 6, 8), highestDailyMax.RecordLow!.Date);

        var lowestDailyMax = dailyExtremes.Metrics.Single(x => x.Label == "Lowest daily maximum");
        Assert.AreEqual("12.3°C", lowestDailyMax.CurrentValue);
        Assert.AreEqual(new DateOnly(2026, 6, 9), lowestDailyMax.CurrentValueDate);

        var highestDailyMin = dailyExtremes.Metrics.Single(x => x.Label == "Highest daily minimum");
        Assert.AreEqual("21.4°C", highestDailyMin.CurrentValue);
        Assert.AreEqual(new DateOnly(2026, 6, 10), highestDailyMin.CurrentValueDate);
        Assert.AreEqual(new DateOnly(2021, 6, 9), highestDailyMin.RecordHigh!.Date);

        var lowestDailyMin = dailyExtremes.Metrics.Single(x => x.Label == "Lowest daily minimum");
        Assert.AreEqual("2.5°C", lowestDailyMin.CurrentValue);
        Assert.AreEqual(new DateOnly(2026, 6, 11), lowestDailyMin.CurrentValueDate);
        Assert.AreEqual(new DateOnly(2002, 6, 11), lowestDailyMin.RecordLow!.Date);
    }

    [TestMethod]
    public async Task GetPrecipitationRecords_DayRecordsMetricHasOccurrence_ExposesCurrentAndHistoricalDates()
    {
        var historicalRecords = CreateHistoricalDailyValues(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 14),
            (year, date) => (year, date.Day) switch
            {
                (2020, 9) => 80d,
                (2000, _) => 2d,
                _ => 10d + ((year - 2000) * 0.1d) + ((date.Day - 8) * 0.01d),
            });
        var service = CreateService(
            historicalRecords: historicalRecords,
            recentValue: date => date.Day == 12 ? 12d : 1d);

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var dailyExtremes = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.DayRecords);
        var highestDaily = dailyExtremes.Metrics.Single(x => x.Label == "Highest daily precipitation");

        Assert.AreEqual("12mm", highestDaily.CurrentValue);
        Assert.AreEqual(new DateOnly(2026, 6, 12), highestDaily.CurrentValueDate);
        Assert.AreEqual(new DateOnly(2020, 6, 9), highestDaily.RecordHigh!.Date);
        Assert.AreEqual(new DateOnly(2000, 6, 8), highestDaily.RecordLow!.Date);
    }

    [TestMethod]
    public async Task GetTemperatureRecords_DayRecordsMetricTies_UsesEarliestOccurrenceDate()
    {
        var historicalMax = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (_, _) => 20d);
        var historicalMin = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (_, _) => 10d);
        var service = CreateTemperatureServiceWithExtremes(
            recentMax: date => date.Day is 10 or 12 ? 42d : 25d,
            recentMin: _ => 10d,
            historicalMax,
            historicalMin);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var dailyExtremes = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.DayRecords);
        var highestDailyMax = dailyExtremes.Metrics.Single(x => x.Label == "Highest daily maximum");

        Assert.AreEqual("42.0°C", highestDailyMax.CurrentValue);
        Assert.AreEqual(new DateOnly(2026, 6, 10), highestDailyMax.CurrentValueDate);
    }

    [TestMethod]
    public async Task DailyTemperatureTileShowsSingleDayMaximumMinimumAndMean()
    {
        // A single day has a maximum, a minimum and a mean — not aggregates across
        // days — so the daily tile has one group (no Period / Daily extremes toggle),
        // each metric compared against the all-time record for that calendar date.
        var historicalMax = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (year, _) => year - 2000);
        var historicalMin = CreateHistoricalDailyValues(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), (year, _) => year - 2040);
        var service = CreateTemperatureServiceWithExtremes(
            recentMax: date => date.Day == 14 ? 30 : date.Day,
            recentMin: date => date.Day == 14 ? -50 : date.Day - 10,
            historicalMax,
            historicalMin);

        var result = await service.GetTemperatureRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var daily = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);

        // Single group → the UI hides the Period / Daily extremes toggle.
        var day = daily.MetricGroups.Single();
        Assert.AreEqual(MetricGroupKey.Day, day.Key);
        CollectionAssert.AreEqual(
            new[] { "Maximum", "Minimum", "Mean" },
            day.Metrics.Select(x => x.Label).ToArray());

        // Each stat shows ONE rank for the observed value (or a New/Equal record badge
        // at an extreme) plus the record high and record low as plain reference values.
        // Today's max (30°C) beats the record high (25°C, 2025) → a new record.
        var maximum = day.Metrics.Single(x => x.Label == "Maximum");
        Assert.AreEqual("30.0°C", maximum.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, maximum.RecordStatus);
        Assert.AreEqual("New record", maximum.RecordStatusText);
        Assert.IsNull(maximum.RankText);
        Assert.AreEqual("Record high", maximum.RecordHigh!.Label);
        Assert.AreEqual("25.0°C", maximum.RecordHigh.Value);
        Assert.AreEqual("2025", maximum.RecordHigh.Year);
        Assert.AreEqual("Record low", maximum.RecordLow!.Label);
        Assert.AreEqual("0.0°C", maximum.RecordLow.Value);
        Assert.AreEqual("2000", maximum.RecordLow.Year);

        // Today's min (-50°C) is below the record low (-40°C, 2000) → a new record.
        var minimum = day.Metrics.Single(x => x.Label == "Minimum");
        Assert.AreEqual("-50.0°C", minimum.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.NewRecord, minimum.RecordStatus);
        Assert.IsNull(minimum.RankText);
        Assert.AreEqual("-15.0°C", minimum.RecordHigh!.Value);
        Assert.AreEqual("-40.0°C", minimum.RecordLow!.Value);
        Assert.AreEqual("2000", minimum.RecordLow.Year);

        // Today's mean (-10°C) sits between the extremes, nearer the low end, so it
        // ranks lowest-first as a single rank; the records are plain references.
        var mean = day.Metrics.Single(x => x.Label == "Mean");
        Assert.AreEqual("-10.0°C", mean.CurrentValue);
        Assert.AreEqual(RecentObservationRecordStatus.None, mean.RecordStatus);
        Assert.AreEqual("11th lowest of 27", mean.RankText);
        Assert.AreEqual("5.0°C", mean.RecordHigh!.Value);
        Assert.AreEqual("-20.0°C", mean.RecordLow!.Value);
    }

    [TestMethod]
    public async Task CompletenessThresholdStripsExpandedRecords()
    {
        var historicalRecords = CreateHistoricalRangeRecords(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 14));
        var service = CreateService(
            historicalRecords: historicalRecords,
            includeRecentRecord: date => date != new DateOnly(2026, 6, 9) && date != new DateOnly(2026, 6, 12));

        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: 1,
            previousMonthCount: 0,
            previousSeasonCount: 0);
        var latestSevenDays = result
            .ApplyCompletenessThreshold(RecentObservationCompletenessThreshold.Default)
            .Tiles
            .Single(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays);
        var total = latestSevenDays.MetricGroups.Single(x => x.Key == MetricGroupKey.PeriodRecords).Metrics.Single();

        Assert.IsFalse(latestSevenDays.HasComparison);
        Assert.IsFalse(total.HasRecords);
        Assert.IsNull(total.RecordHigh);
        Assert.IsNull(total.RecordLow);
        Assert.IsNull(total.RankText);
        Assert.AreEqual(RecentObservationRecordStatus.None, total.RecordStatus);
        Assert.IsNotNull(total.CurrentValue);
    }

    private static RecentObservationsService CreateTemperatureServiceWithExtremes(
        Func<DateOnly, double> recentMax,
        Func<DateOnly, double> recentMin,
        List<DataRecord> historicalMax,
        List<DataRecord> historicalMin)
    {
        var dataService = new Mock<IDataService>();
        SetupEmptyClimateRecords(dataService);

        dataService
            .Setup(x => x.GetRecentObservations(LocationId, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                TempMax = new RecentObservationSeries
                {
                    Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), recentMax),
                },
                TempMin = new RecentObservationSeries
                {
                    Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), recentMin),
                },
            });
        dataService
            .Setup(x => x.GetClimateRecords(
                LocationId,
                DataType.TempMax,
                DataAdjustment.Unadjusted,
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()))
            .ReturnsAsync(CreateClimateRecordsResponse(DataType.TempMax, DataAdjustment.Unadjusted, historicalMax));
        dataService
            .Setup(x => x.GetClimateRecords(
                LocationId,
                DataType.TempMin,
                DataAdjustment.Unadjusted,
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()))
            .ReturnsAsync(CreateClimateRecordsResponse(DataType.TempMin, DataAdjustment.Unadjusted, historicalMin));

        return CreateRecentObservationsService(dataService);
    }

    private static List<DataRecord> CreateHistoricalDailyValues(
        DateOnly templateStart,
        DateOnly templateEnd,
        Func<int, DateOnly, double> getValue,
        int startYear = 2000,
        int endYear = 2025)
    {
        var records = new List<DataRecord>();
        for (var year = startYear; year <= endYear; year++)
        {
            for (var day = templateStart.Day; day <= templateEnd.Day; day++)
            {
                var date = new DateOnly(year, templateStart.Month, day);
                records.Add(new DataRecord(date, getValue(year, date)));
            }
        }

        return records;
    }

    private static async Task<List<RecentObservationTileViewModel>> GetGeneratedTiles(RecentObservationsService service, int previousYearCount = 0)
    {
        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.MaximumPreviousDayCount,
            previousMonthCount: RecentObservationPeriodSelection.MaximumPreviousMonthCount,
            previousSeasonCount: RecentObservationPeriodSelection.MaximumPreviousSeasonCount,
            previousYearCount: previousYearCount);

        return result.Tiles;
    }

    private static void AssertMetricGroupLabel(
        RecentObservationsTabResult result,
        RecentObservationPeriodKind periodKind,
        int? periodOffset,
        string expectedLabel)
    {
        var tile = result.Tiles.Single(x => x.PeriodKind == periodKind && (!periodOffset.HasValue || x.PeriodOffset == periodOffset));
        var periodGroup = tile.MetricGroups.Single(x => x.Key == MetricGroupKey.PeriodRecords);

        Assert.AreEqual(expectedLabel, periodGroup.Title, $"Unexpected period metric group title for {tile.PeriodTitle}.");
    }

    private static void AssertPrecipitationHistoricalRange(RecentObservationTileViewModel tile)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(tile.HistoricalMaxLabel), $"Expected wettest label for {tile.PeriodTitle}.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(tile.HistoricalMaxValue), $"Expected wettest value for {tile.PeriodTitle}.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(tile.HistoricalMinLabel), $"Expected driest label for {tile.PeriodTitle}.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(tile.HistoricalMinValue), $"Expected driest value for {tile.PeriodTitle}.");
        StringAssert.StartsWith(tile.HistoricalMaxLabel!, "Wettest ");
        StringAssert.StartsWith(tile.HistoricalMinLabel!, "Driest ");
        Assert.AreEqual(
            tile.HistoricalMaxLabel!["Wettest ".Length..],
            tile.HistoricalMinLabel!["Driest ".Length..],
            $"Wettest and driest labels should use the same comparable period text for {tile.PeriodTitle}.");
    }

    private static IEnumerable<int> GetAvailableOffsets(
        IEnumerable<RecentObservationTileViewModel> tiles,
        RecentObservationPeriodKind periodKind)
    {
        return tiles
            .Where(tile => tile.PeriodKind == periodKind && tile.PeriodOffset.HasValue)
            .Select(tile => tile.PeriodOffset!.Value)
            .Order();
    }

    private static RecentObservationTileViewModel GetTile(
        IEnumerable<RecentObservationTileViewModel> tiles,
        RecentObservationPeriodKind periodKind,
        int periodOffset)
    {
        return tiles.Single(tile => tile.PeriodKind == periodKind && tile.PeriodOffset == periodOffset);
    }

    private static IEnumerable<RecentObservationTileViewModel> CreateOrderedDynamicTiles(RecentObservationPeriodKind periodKind, int count)
    {
        for (var offset = 1; offset <= count; offset++)
        {
            var title = periodKind == RecentObservationPeriodKind.Daily
                ? offset switch
                {
                    1 => "Today",
                    2 => "Yesterday",
                    3 => "12 June",
                    4 => "11 June",
                    _ => $"{15 - offset} June",
                }
                : $"{periodKind} {offset}";

            yield return CreateTile(periodKind, offset, title);
        }
    }

    private static RecentObservationTileViewModel CreateTile(RecentObservationPeriodKind periodKind, int? periodOffset, string periodTitle)
    {
        var startDate = periodOffset.HasValue
            ? new DateOnly(2026, 6, Math.Max(1, 15 - periodOffset.Value))
            : new DateOnly(2026, 6, 14);

        return new RecentObservationTileViewModel
        {
            PeriodKind = periodKind,
            PeriodOffset = periodOffset,
            PeriodStartDate = startDate,
            PeriodEndDate = startDate,
            PeriodTitle = periodTitle,
        };
    }

    private static RecentObservationsService CreateService(
        DateOnly? recentStartDate = null,
        DateOnly? recentEndDate = null,
        List<DataRecord>? historicalRecords = null,
        Func<DateOnly, bool>? includeRecentRecord = null,
        DateOnly? today = null,
        Func<DateOnly, double>? recentValue = null)
    {
        var dataService = new Mock<IDataService>();
        SetupEmptyClimateRecords(dataService);

        dataService
            .Setup(x => x.GetRecentObservations(LocationId, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Precipitation = new RecentObservationSeries
                {
                    Records = CreateDailyRecords(
                        recentStartDate ?? new DateOnly(2025, 7, 1),
                        recentEndDate ?? new DateOnly(2026, 6, 14),
                        recentValue ?? (_ => 1d),
                        includeRecentRecord),
                },
            });

        if (historicalRecords is not null)
        {
            dataService
                .Setup(x => x.GetClimateRecords(
                    LocationId,
                    DataType.Precipitation,
                    null,
                    It.IsAny<bool>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    false,
                    It.IsAny<int?>()))
                .ReturnsAsync(CreateClimateRecordsResponse(DataType.Precipitation, null, historicalRecords));
        }

        return CreateRecentObservationsService(dataService, today);
    }

    private static RecentObservationsService CreateTemperatureService(
        List<DataRecord> historicalRecords,
        Func<DateOnly, bool>? includeMaxRecord = null,
        Func<DateOnly, bool>? includeMinRecord = null,
        List<DataRecord>? historicalMaxRecords = null,
        List<DataRecord>? historicalMinRecords = null,
        Func<DateOnly, double>? recentMax = null,
        Func<DateOnly, double>? recentMin = null)
    {
        var dataService = new Mock<IDataService>();
        SetupEmptyClimateRecords(dataService);

        dataService
            .Setup(x => x.GetRecentObservations(LocationId, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                TempMax = new RecentObservationSeries
                {
                    Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), recentMax ?? (_ => 20d), includeMaxRecord),
                },
                TempMin = new RecentObservationSeries
                {
                    Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), recentMin ?? (_ => 10d), includeMinRecord),
                },
            });
        dataService
            .Setup(x => x.GetClimateRecords(
                LocationId,
                DataType.TempMean,
                DataAdjustment.Unadjusted,
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()))
            .ReturnsAsync(CreateClimateRecordsResponse(DataType.TempMean, DataAdjustment.Unadjusted, historicalRecords));

        if (historicalMaxRecords is not null)
        {
            dataService
                .Setup(x => x.GetClimateRecords(
                    LocationId,
                    DataType.TempMax,
                    DataAdjustment.Unadjusted,
                    It.IsAny<bool>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    false,
                    It.IsAny<int?>()))
                .ReturnsAsync(CreateClimateRecordsResponse(DataType.TempMax, DataAdjustment.Unadjusted, historicalMaxRecords));
        }

        if (historicalMinRecords is not null)
        {
            dataService
                .Setup(x => x.GetClimateRecords(
                    LocationId,
                    DataType.TempMin,
                    DataAdjustment.Unadjusted,
                    It.IsAny<bool>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    false,
                    It.IsAny<int?>()))
                .ReturnsAsync(CreateClimateRecordsResponse(DataType.TempMin, DataAdjustment.Unadjusted, historicalMinRecords));
        }

        return CreateRecentObservationsService(dataService);
    }

    private static void SetupEmptyClimateRecords(Mock<IDataService> dataService)
    {
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
    }

    private static RecentObservationsService CreateRecentObservationsService(Mock<IDataService> dataService, DateOnly? today = null)
    {
        var currentDate = today ?? new DateOnly(2026, 6, 14);

        return new RecentObservationsService(
            new RecentObservationsDataProvider(dataService.Object),
            new RecentObservationsCalculator(new FixedTimeProvider(new DateTimeOffset(currentDate.Year, currentDate.Month, currentDate.Day, 12, 0, 0, TimeSpan.Zero))));
    }

    private static ClimateRecordsResponse CreateClimateRecordsResponse(DataType dataType, DataAdjustment? dataAdjustment, List<DataRecord> records)
    {
        return new ClimateRecordsResponse
        {
            DataType = dataType,
            DataAdjustment = dataAdjustment,
            DataResolution = DataResolution.Daily,
            Records = records,
            StartYear = records.Min(x => (int)x.Year),
            EndYear = records.Max(x => (int)x.Year),
            TotalCount = records.Count,
        };
    }

    private static List<DataRecord> CreateDailyRecords(DateOnly startDate, DateOnly endDate)
    {
        return CreateDailyRecords(startDate, endDate, _ => 1d);
    }

    private static List<DataRecord> CreateDailyRecords(
        DateOnly startDate,
        DateOnly endDate,
        Func<DateOnly, double> getValue,
        Func<DateOnly, bool>? includeRecord = null)
    {
        var records = new List<DataRecord>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (includeRecord is not null && !includeRecord(date))
            {
                continue;
            }

            records.Add(new DataRecord(date, getValue(date)));
        }

        return records;
    }

    private static List<DataRecord> CreateHistoricalSameDateRecords(DateOnly templateDate, int startYear, int endYear, double valueOffset)
    {
        var records = new List<DataRecord>();
        for (var year = startYear; year <= endYear; year++)
        {
            var date = new DateOnly(year, templateDate.Month, templateDate.Day);
            records.Add(new DataRecord(date, valueOffset + year - startYear + 1));
        }

        return records;
    }

    private static List<DataRecord> CreateHistoricalRangeRecords(
        DateOnly templateStart,
        DateOnly templateEnd,
        int startYear = 2000,
        int endYear = 2025,
        double valueOffset = 0d)
    {
        var records = new List<DataRecord>();
        for (var year = startYear; year <= endYear; year++)
        {
            var startDate = new DateOnly(year, templateStart.Month, templateStart.Day);
            var endDate = new DateOnly(year, templateEnd.Month, templateEnd.Day);
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                records.Add(new DataRecord(date, valueOffset + year - startYear + date.Day));
            }
        }

        return records;
    }

    private static List<DataRecord> CreateHistoricalComparisonModeRecords(DateOnly templateStart, DateOnly templateEnd)
    {
        var records = new List<DataRecord>();
        for (var year = 1950; year <= 2005; year++)
        {
            var value = year switch
            {
                < 1980 => 1d,
                1980 => 10d,
                _ => 100d,
            };

            var startDate = new DateOnly(year, templateStart.Month, templateStart.Day);
            var endDate = new DateOnly(year, templateEnd.Month, templateEnd.Day);
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                records.Add(new DataRecord(date, value));
            }
        }

        return records;
    }

    private static Location CreateSouthernHemisphereLocation()
    {
        return CreateLocation(-35.3d);
    }

    private static Location CreateSouthernHemisphereLocation(Guid locationId)
    {
        var location = CreateLocation(-35.3d);
        location.Id = locationId;
        return location;
    }

    private static Location CreateLocation(double latitude)
    {
        return new Location
        {
            Id = LocationId,
            Name = latitude < 0d ? "Canberra" : "New York",
            CountryCode = latitude < 0d ? "AU" : "US",
            Coordinates = new Coordinates(latitude, latitude < 0d ? 149.1d : -74.0d),
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
