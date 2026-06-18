namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;

public sealed class RecentObservationsService : IRecentObservationsService
{
    private readonly IRecentObservationsDataProvider dataProvider;
    private readonly IRecentObservationsCalculator calculator;

    public RecentObservationsService(
        IRecentObservationsDataProvider dataProvider,
        IRecentObservationsCalculator calculator)
    {
        this.dataProvider = dataProvider;
        this.calculator = calculator;
    }

    public Task<RecentObservationsDataSet> LoadTemperatureData(Location location)
    {
        return dataProvider.LoadTemperatureData(location);
    }

    public Task<RecentObservationsDataSet> LoadPrecipitationData(Location location)
    {
        return dataProvider.LoadPrecipitationData(location);
    }

    public RecentObservationsTabResult Calculate(
        Location location,
        RecentObservationsDataSet dataSet,
        RecentObservationsOptions options)
    {
        return calculator.Calculate(location, dataSet, options);
    }

    public async Task<RecentObservationsTabResult> GetTemperatureRecords(
        Location location,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        DateOnly? referenceDate = null,
        ComparisonEndMode comparisonEndMode = ComparisonEndMode.FullDataset)
    {
        var dataSet = await LoadTemperatureData(location);
        return Calculate(location, dataSet, CreateOptions(previousDayCount, previousMonthCount, previousSeasonCount, referenceDate, comparisonEndMode));
    }

    public async Task<RecentObservationsTabResult> GetPrecipitationRecords(
        Location location,
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
        DateOnly? referenceDate = null,
        ComparisonEndMode comparisonEndMode = ComparisonEndMode.FullDataset)
    {
        var dataSet = await LoadPrecipitationData(location);
        return Calculate(location, dataSet, CreateOptions(previousDayCount, previousMonthCount, previousSeasonCount, referenceDate, comparisonEndMode));
    }

    private static RecentObservationsOptions CreateOptions(
        int previousDayCount,
        int previousMonthCount,
        int previousSeasonCount,
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
        };
    }
}
