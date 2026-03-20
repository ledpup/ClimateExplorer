namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using static ClimateExplorer.Core.Enums;

public static class ClimateDataHelper
{
    public static async Task<LocationAnomalySummary?> CalculateAnomaly(
        IDataService dataService,
        IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions,
        Location location,
        List<DataSubstitute> dataSubstitutes,
        ContainerAggregationFunctions function,
        BinGranularities binGranularity = BinGranularities.ByYear)
    {
        var series = await GetData(dataService, dataSetDefinitions, location, dataSubstitutes, function, binGranularity);

        if (series == null || series.DataRecords.Count == 0)
        {
            return null;
        }

        var average = series.DataRecords.Average(x => x.Value)!.Value;

        var anomalyRecords =
            series.DataRecords
            .Where(x => x.Value != null)
            .Select(x => new YearlyValues(
                ((YearBinIdentifier)BinIdentifier.Parse(x.BinId!)).Year,
                x.Value!.Value - average,
                x.Value!.Value,
                x.Value!.Value / average * 100D))
            .ToList();

        var anomaly = AnomalyCalculator.CalculateAnomaly(series.DataRecords);

        return new LocationAnomalySummary { CalculatedAnomaly = anomaly, DataSet = series, AnomalyRecords = anomalyRecords };
    }

    public static async Task<DataSet?> GetData(
        IDataService dataService,
        IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions,
        Location location,
        List<DataSubstitute> dataSubstitutes,
        ContainerAggregationFunctions function,
        BinGranularities binGranularity = BinGranularities.ByYear)
    {
        var measurementForLocation =
            DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
                dataSetDefinitions,
                location.Id,
                dataSubstitutes,
                throwIfNoMatch: false)!;

        if (measurementForLocation == null)
        {
            return null;
        }

        var spec = new SeriesSpecification
        {
            DataAdjustment = measurementForLocation.MeasurementDefinition!.DataAdjustment,
            DataSetDefinitionId = measurementForLocation.DataSetDefinition!.Id,
            DataType = measurementForLocation.MeasurementDefinition.DataType,
            LocationId = location.Id,
        };

        var series =
            await dataService.PostDataSet(
                binGranularity,
                function,
                function,
                function,
                SeriesValueOptions.Value,
                [spec],
                SeriesDerivationTypes.ReturnSingleSeries,
                1.0f,
                1.0f,
                0.7f,
                14,
                SeriesTransformations.Identity);

        return series!;
    }
}
