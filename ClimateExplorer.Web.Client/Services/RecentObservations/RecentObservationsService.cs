namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using static ClimateExplorer.Core.Enums;

public sealed class RecentObservationsService : IRecentObservationsService
{
    private readonly IRecentObservationsDataProvider dataProvider;
    private readonly IRecentObservationsCalculator calculator;
    private readonly Dictionary<(RecentObservationsDataSet DataSet, RecentObservationsOptions Options, double? Latitude), RecentObservationsTabResult> resultCache = [];

    public RecentObservationsService(
        IRecentObservationsDataProvider dataProvider,
        IRecentObservationsCalculator calculator)
    {
        this.dataProvider = dataProvider;
        this.calculator = calculator;
    }

    public Task<RecentObservationsDataSet> LoadTemperatureData(Location location, DataAdjustment? preferredAdjustment = DataAdjustment.Unadjusted)
    {
        return dataProvider.LoadTemperatureData(location, preferredAdjustment);
    }

    public Task<RecentObservationsDataSet> LoadPrecipitationData(Location location)
    {
        return dataProvider.LoadPrecipitationData(location);
    }

    public Task<RecentObservationsDataSet> LoadData(Guid contextId, ObservationDomain domain, DataAdjustment? preferredAdjustment = null)
    {
        return dataProvider.LoadData(contextId, domain, preferredAdjustment);
    }

    public RecentObservationsTabResult Calculate(
        Location location,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options)
    {
        return Calculate(location.Coordinates.Latitude, dataSet, options);
    }

    public RecentObservationsTabResult Calculate(
        double? latitude,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options)
    {
        var key = (dataSet, options, latitude);
        if (resultCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var result = calculator.Calculate(latitude, dataSet, options);
        resultCache[key] = result;
        return result;
    }

    public async Task<RecentObservationsTabResult> GetTemperatureRecords(
        Location location,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        int previousYearCount = 0,
        DateOnly? referenceDate = null,
        ComparisonEndMode comparisonEndMode = ComparisonEndMode.FullDataset)
    {
        var dataSet = await LoadTemperatureData(location);
        return Calculate(location, dataSet, CreateOptions(previousDayCount, previousMonthCount, previousSeasonCount, previousYearCount, referenceDate, comparisonEndMode));
    }

    public async Task<RecentObservationsTabResult> GetPrecipitationRecords(
        Location location,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        int previousYearCount = 0,
        DateOnly? referenceDate = null,
        ComparisonEndMode comparisonEndMode = ComparisonEndMode.FullDataset)
    {
        var dataSet = await LoadPrecipitationData(location);
        return Calculate(location, dataSet, CreateOptions(previousDayCount, previousMonthCount, previousSeasonCount, previousYearCount, referenceDate, comparisonEndMode));
    }

    private static RecentObservationsOptions CreateOptions(
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        int previousYearCount,
        DateOnly? referenceDate,
        ComparisonEndMode comparisonEndMode)
    {
        return new RecentObservationsOptions
        {
            ReferenceDate = referenceDate,
            ComparisonEndMode = comparisonEndMode,
            PreviousDayCount = previousDayCount,
            PreviousMonthCount = previousMonthCount,
            PreviousSeasonCount = previousSeasonCount,
            PreviousYearCount = previousYearCount,
        };
    }
}
