namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class RecentObservationsUnitsTests
{
    private static readonly Guid LocationId = Guid.Parse("50000000-0000-0000-0000-000000000003");
    private static readonly DateOnly Today = new(2026, 6, 14);

    [TestMethod]
    public async Task Calculate_ShiftedTemperatureInput_LevelsMoveAndChangesDoNot()
    {
        // Covers every formatted number on the tiles and their expanded tabs: values, records,
        // ranges and averages (levels), and anomalies, typical variation and trend rates (changes).
        var original = await RecordMeasures(shift: 0);
        var shifted = await RecordMeasures(shift: 10);

        OffsetShift.AssertLevelsMoveAndChangesDoNot(original, shifted, 10, minimumLevels: 20, minimumChanges: 10);
    }

    [TestMethod]
    public async Task Calculate_UsCustomary_AnomalyIsScaledNotOffset()
    {
        var metric = await CalculateTiles(UnitFormatter.Metric, shift: 0);
        var usCustomary = await CalculateTiles(UnitFormatter.UsCustomary, shift: 0);

        var metricAnomaly = AnomalyStat(metric);
        var usAnomaly = AnomalyStat(usCustomary);

        Assert.IsNotNull(metricAnomaly.AnomalyValue);
        Assert.AreEqual(UnitFormatter.UsCustomary.Format(Measure.Change(metricAnomaly.AnomalyValue!.Value, UnitOfMeasure.DegreesCelsius), signed: true), usAnomaly.Value);
        StringAssert.EndsWith(usAnomaly.Value, "°F");
        StringAssert.EndsWith(metricAnomaly.Value, "°C");

        // The comparison itself is calculated in metric either way.
        Assert.AreEqual(metricAnomaly.AnomalyValue, usAnomaly.AnomalyValue);
    }

    private static RecentObservationStatViewModel AnomalyStat(RecentObservationsTabResult result)
    {
        return result.Tiles.SelectMany(x => x.Stats).First(x => x.Label == "Anomaly");
    }

    private static async Task<List<Measure>> RecordMeasures(double shift)
    {
        var units = new RecordingUnitFormatter(UnitFormatter.UsCustomary);
        var result = await CalculateTiles(units, shift);

        // The trend tab is built only when it's viewed.
        foreach (var trendTab in result.Tiles.SelectMany(x => x.ExpandedTabs).OfType<RecentObservationTrendTabViewModel>())
        {
            _ = trendTab.Metrics;
        }

        return units.Measures;
    }

    private static async Task<RecentObservationsTabResult> CalculateTiles(IUnitFormatter units, double shift)
    {
        var dataService = new Mock<IDataService>();

        foreach (var dataType in new[] { DataType.TempMax, DataType.TempMin, DataType.TempMean })
        {
            var records = CreateDailyRecords(dataType, shift);

            dataService
                .Setup(x => x.GetClimateRecords(
                    It.IsAny<Guid>(),
                    dataType,
                    It.IsAny<DataAdjustment?>(),
                    It.IsAny<bool>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(new ClimateRecordsResponse
                {
                    DataType = dataType,
                    DataAdjustment = DataAdjustment.Unadjusted,
                    DataResolution = DataResolution.Daily,
                    UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                    Records = records,
                    StartYear = records.Min(x => (int)x.Year),
                    EndYear = records.Max(x => (int)x.Year),
                    TotalCount = records.Count,
                });
        }

        var service = new RecentObservationsService(
            new RecentObservationsDataProvider(dataService.Object),
            new RecentObservationsCalculator(new FixedTimeProvider(new DateTimeOffset(Today.Year, Today.Month, Today.Day, 12, 0, 0, TimeSpan.Zero))));

        var location = new Location
        {
            Id = LocationId,
            Name = "Canberra",
            CountryCode = "AU",
            Coordinates = new Coordinates(-35.3d, 149.1d),
        };

        var dataSet = await service.LoadTemperatureData(location);

        return service.Calculate(
            location,
            dataSet,
            new RecentObservationsOptions
            {
                ReferenceDate = Today,
                ComparisonEndMode = ComparisonEndMode.FullDataset,
                PreviousDayCount = 2,
                PreviousMonthCount = 1,
                PreviousSeasonCount = 1,
                PreviousYearCount = 1,
                Units = units,
            });
    }

    // Daily values from 1950 to today with a warming trend, a seasonal cycle and year-to-year variation,
    // so rankings, records, spreads and trends are all non-trivial.
    private static List<DataRecord> CreateDailyRecords(DataType dataType, double shift)
    {
        var offset = dataType switch
        {
            DataType.TempMax => 5d,
            DataType.TempMin => -5d,
            _ => 0d,
        };

        var records = new List<DataRecord>();

        for (var date = new DateOnly(1950, 1, 1); date <= Today; date = date.AddDays(1))
        {
            var value = 15
                + offset
                + ((date.Year - 1950) * 0.03)
                + (8 * Math.Cos((date.DayOfYear - 15) / 365.25 * 2 * Math.PI))
                + (2 * Math.Sin(date.DayNumber * 0.37))
                + shift;

            records.Add(new DataRecord(date, value));
        }

        return records;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
