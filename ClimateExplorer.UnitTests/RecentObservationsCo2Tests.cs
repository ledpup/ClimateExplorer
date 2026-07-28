namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class RecentObservationsCo2Tests
{
    private static readonly Guid ContextId = RecentObservationsContext.ForAtmosphere().Id;

    [TestMethod]
    public async Task LoadData_Co2Domain_RequestsOnlyNullAdjustment()
    {
        var dataService = new Mock<IDataService>();
        dataService
            .Setup(x => x.GetClimateRecords(
                ContextId,
                DataType.CO2,
                null,
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()))
            .ReturnsAsync(CreateResponse(CreateDailyRecords(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14))));
        var service = CreateService(dataService);

        var dataSet = await service.LoadData(ContextId, ObservationDomainCatalog.Co2, preferredAdjustment: null);

        Assert.IsTrue(dataSet.IsSupported);
        Assert.AreEqual(ObservationDomainCatalog.Co2Key, dataSet.DomainKey);

        // Regression test for the latent bug: a non-adjustment domain must request the single
        // `null` adjustment candidate, never falling through to DataAdjustment.Unadjusted.
        dataService.Verify(
            x => x.GetClimateRecords(
                ContextId,
                DataType.CO2,
                DataAdjustment.Unadjusted,
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Calculate_Co2Domain_ProducesNoSeasonTilesAndFormatsPpm()
    {
        var dataService = new Mock<IDataService>();
        dataService
            .Setup(x => x.GetClimateRecords(
                ContextId,
                DataType.CO2,
                null,
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                false,
                It.IsAny<int?>()))
            .ReturnsAsync(CreateResponse(CreateDailyRecords(new DateOnly(2020, 1, 1), new DateOnly(2026, 6, 14))));
        var service = CreateService(dataService);

        var dataSet = await service.LoadData(ContextId, ObservationDomainCatalog.Co2);
        var result = service.Calculate(
            (double?)null,
            dataSet,
            new RecentObservationsOptions
            {
                ReferenceDate = new DateOnly(2026, 6, 14),
                ComparisonEndMode = ComparisonEndMode.FullDataset,
                PreviousDayCount = 1,
                PreviousMonthCount = 2,
                PreviousSeasonCount = 3,
                PreviousYearCount = 2,
            });

        Assert.IsFalse(result.Tiles.Any(x => x.PeriodKind is RecentObservationPeriodKind.CurrentSeason or RecentObservationPeriodKind.PreviousSeason));
        Assert.IsTrue(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.Daily));
        Assert.IsTrue(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.LatestSevenDays));
        Assert.IsTrue(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.CurrentMonth));
        Assert.IsTrue(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.PreviousMonth));
        Assert.IsTrue(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.YearToDate));
        Assert.IsTrue(result.Tiles.Any(x => x.PeriodKind == RecentObservationPeriodKind.PreviousYear));

        var dailyTile = result.Tiles.Single(x => x.PeriodKind == RecentObservationPeriodKind.Daily);
        StringAssert.EndsWith(dailyTile.PrimaryValue, " ppm");
        Assert.AreEqual("Mean CO₂", dailyTile.PrimaryLabel);
    }

    private static RecentObservationsService CreateService(Mock<IDataService> dataService)
    {
        return new RecentObservationsService(
            new RecentObservationsDataProvider(dataService.Object),
            new RecentObservationsCalculator());
    }

    private static ClimateRecordsResponse CreateResponse(List<DataRecord> records)
    {
        return new ClimateRecordsResponse
        {
            DataType = DataType.CO2,
            DataAdjustment = null,
            DataResolution = DataResolution.Daily,
            Records = records,
            StartYear = records.Min(x => (int)x.Year),
            EndYear = records.Max(x => (int)x.Year),
            TotalCount = records.Count,
        };
    }

    private static List<DataRecord> CreateDailyRecords(DateOnly startDate, DateOnly endDate)
    {
        var records = new List<DataRecord>();
        var value = 400d;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            records.Add(new DataRecord(date, value));
            value += 0.01d;
        }

        return records;
    }
}
