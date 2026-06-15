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
    public async Task GetPrecipitationRecordsComparesGeneratedDaysAgainstHistoricalSameCalendarDate()
    {
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
        Assert.AreEqual("Wettest 13 Jun", generatedDay.HistoricalMaxLabel);
        Assert.AreEqual("26mm", generatedDay.HistoricalMaxValue);
        Assert.IsNull(generatedDay.HistoricalMinLabel);
    }

    [TestMethod]
    public async Task GetTemperatureRecordsGeneratedDaysKeepTemperatureStatsAndHistoricalRange()
    {
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
        CollectionAssert.AreEqual(new[] { "Max temp", "Min temp", "Historical average", "Anomaly" }, generatedDay.Stats.Select(x => x.Label).ToArray());
        Assert.AreEqual("Warmest 13 Jun", generatedDay.HistoricalMaxLabel);
        Assert.AreEqual("36.0°C", generatedDay.HistoricalMaxValue);
        Assert.AreEqual("Coolest 13 Jun", generatedDay.HistoricalMinLabel);
        Assert.AreEqual("11.0°C", generatedDay.HistoricalMinValue);
    }

    [TestMethod]
    public async Task PeriodSelectionCreatesAddButtonLabelsFromGeneratedTiles()
    {
        var service = CreateService();
        var tiles = await GetGeneratedTiles(service);
        var selection = new RecentObservationPeriodSelection();

        Assert.AreEqual("Add Yesterday", selection.CreateAddButtonLabel(RecentObservationPeriodKind.Daily, tiles, "day"));
        Assert.AreEqual("Add May 2026", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousMonth, tiles, "month"));
        Assert.AreEqual("Add Autumn 2026", selection.CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, tiles, "season"));
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
        var lastWeek = CreateTile(RecentObservationPeriodKind.LastWeek, null, "Last week");
        var currentMonth = CreateTile(RecentObservationPeriodKind.CurrentMonth, null, "June 2026 to date");
        var previousMonth = CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026");
        var currentSeason = CreateTile(RecentObservationPeriodKind.CurrentSeason, null, "Winter to Date");
        var previousSeason = CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026");
        var yearToDate = CreateTile(RecentObservationPeriodKind.YearToDate, null, "2026 to date");

        selection.AddEarlierDay();
        selection.AddEarlierMonth();
        selection.AddEarlierSeason();

        Assert.IsFalse(selection.IsRemovable(currentDay));
        Assert.IsTrue(selection.IsRemovable(previousDay));
        Assert.IsFalse(selection.IsRemovable(lastWeek));
        Assert.IsFalse(selection.IsRemovable(currentMonth));
        Assert.IsTrue(selection.IsRemovable(previousMonth));
        Assert.IsFalse(selection.IsRemovable(currentSeason));
        Assert.IsTrue(selection.IsRemovable(previousSeason));
        Assert.IsFalse(selection.IsRemovable(yearToDate));
    }

    [TestMethod]
    public void PeriodSelectionRemovesSpecificDynamicTiles()
    {
        var selection = new RecentObservationPeriodSelection();
        var previousDay = CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday");
        var previousMonth = CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026");
        var previousSeason = CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026");

        selection.AddEarlierDay();
        selection.AddEarlierDay();
        selection.AddEarlierMonth();
        selection.AddEarlierMonth();
        selection.AddEarlierSeason();

        selection.Remove(previousDay);
        selection.Remove(previousMonth);
        selection.Remove(previousSeason);

        Assert.IsFalse(selection.IsVisible(previousDay));
        Assert.IsTrue(selection.IsVisible(CreateTile(RecentObservationPeriodKind.Daily, 3, "12 June")));
        Assert.IsFalse(selection.IsVisible(previousMonth));
        Assert.IsTrue(selection.IsVisible(CreateTile(RecentObservationPeriodKind.PreviousMonth, 2, "April 2026")));
        Assert.IsFalse(selection.IsVisible(previousSeason));
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
        }

        Assert.AreEqual(RecentObservationPeriodSelection.MaximumPreviousDayCount, selection.PreviousDayCount);
        Assert.AreEqual(RecentObservationPeriodSelection.MaximumPreviousMonthCount, selection.PreviousMonthCount);
        Assert.AreEqual(RecentObservationPeriodSelection.MaximumPreviousSeasonCount, selection.PreviousSeasonCount);
        Assert.IsTrue(selection.IsAddEarlierDayDisabled);
        Assert.IsTrue(selection.IsAddEarlierMonthDisabled);
        Assert.IsTrue(selection.IsAddEarlierSeasonDisabled);
        Assert.IsFalse(selection.CanAddEarlierDay(Enumerable.Range(1, 20)));
        Assert.IsFalse(selection.CanAddEarlierMonth(Enumerable.Range(1, 20)));
        Assert.IsFalse(selection.CanAddEarlierSeason(Enumerable.Range(1, 20)));
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
            CreateTile(RecentObservationPeriodKind.LastWeek, null, "Last week"),
            CreateTile(RecentObservationPeriodKind.CurrentMonth, null, "June 2026 to date"),
            CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026"),
            CreateTile(RecentObservationPeriodKind.PreviousMonth, 2, "April 2026"),
            CreateTile(RecentObservationPeriodKind.CurrentSeason, null, "Winter to Date"),
            CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026"),
            CreateTile(RecentObservationPeriodKind.YearToDate, null, "2026 to date"),
        };

        selection.AddEarlierDay();
        selection.AddEarlierMonth();
        selection.AddEarlierMonth();
        selection.Remove(CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026"));
        selection.AddEarlierSeason();

        CollectionAssert.AreEqual(
            new[]
            {
                "Today",
                "Yesterday",
                "Last week",
                "June 2026 to date",
                "April 2026",
                "Winter to Date",
                "Autumn 2026",
                "2026 to date",
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
        Assert.IsFalse(selection.IsAddEarlierDayDisabled);
        Assert.IsFalse(selection.IsAddEarlierMonthDisabled);
        Assert.IsFalse(selection.IsAddEarlierSeasonDisabled);
        Assert.IsTrue(selection.IsVisible(CreateTile(RecentObservationPeriodKind.Daily, 1, "Today")));
        Assert.IsFalse(selection.IsVisible(CreateTile(RecentObservationPeriodKind.Daily, 2, "Yesterday")));
        Assert.IsFalse(selection.IsVisible(CreateTile(RecentObservationPeriodKind.PreviousMonth, 1, "Last month - May 2026")));
        Assert.IsFalse(selection.IsVisible(CreateTile(RecentObservationPeriodKind.PreviousSeason, 1, "Autumn 2026")));
    }

    private static async Task<List<RecentObservationTileViewModel>> GetGeneratedTiles(RecentObservationsService service)
    {
        var result = await service.GetPrecipitationRecords(
            CreateSouthernHemisphereLocation(),
            previousDayCount: RecentObservationPeriodSelection.MaximumPreviousDayCount,
            previousMonthCount: RecentObservationPeriodSelection.MaximumPreviousMonthCount,
            previousSeasonCount: RecentObservationPeriodSelection.MaximumPreviousSeasonCount);

        return result.Tiles;
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
        List<DataRecord>? historicalRecords = null)
    {
        var dataService = new Mock<IDataService>();
        SetupEmptyClimateRecords(dataService);

        dataService
            .Setup(x => x.GetRecentObservations(LocationId, DataType.Precipitation, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Records = CreateDailyRecords(
                    recentStartDate ?? new DateOnly(2025, 7, 1),
                    recentEndDate ?? new DateOnly(2026, 6, 14)),
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

        return CreateRecentObservationsService(dataService);
    }

    private static RecentObservationsService CreateTemperatureService(List<DataRecord> historicalRecords)
    {
        var dataService = new Mock<IDataService>();
        SetupEmptyClimateRecords(dataService);

        dataService
            .Setup(x => x.GetRecentObservations(LocationId, DataType.TempMax, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), _ => 20d),
            });
        dataService
            .Setup(x => x.GetRecentObservations(LocationId, DataType.TempMin, false))
            .ReturnsAsync(new RecentObservationsResponse
            {
                IsSupported = true,
                Records = CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), _ => 10d),
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

    private static RecentObservationsService CreateRecentObservationsService(Mock<IDataService> dataService)
    {
        return new RecentObservationsService(
            dataService.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 6, 14, 12, 0, 0, TimeSpan.Zero)));
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

    private static List<DataRecord> CreateDailyRecords(DateOnly startDate, DateOnly endDate, Func<DateOnly, double> getValue)
    {
        var records = new List<DataRecord>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
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
