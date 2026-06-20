namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

public sealed class LocationDefaultChartProvider : ILocationDefaultChartProvider
{
    public ChartState CreateDefault(LocationDefaultChartContext context)
    {
        var series = new List<ChartSeriesDefinition>();
        var temperature = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
            context.DataSetDefinitions,
            context.Location.Id,
            DataSubstitute.StandardTemperatureDataMatches(),
            throwIfNoMatch: true)!;

        series.Add(
            new ChartSeriesDefinition
            {
                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(context.Location, temperature),
                Aggregation = SeriesAggregationOptions.Mean,
                BinGranularity = BinGranularities.ByYear,
                Smoothing = SeriesSmoothingOptions.MovingAverage,
                SmoothingWindow = 20,
                Value = SeriesValueOptions.Value,
                Year = null,
            });

        var precipitation = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
            context.DataSetDefinitions,
            context.Location.Id,
            DataType.Precipitation,
            null,
            throwIfNoMatch: false);

        if (precipitation is not null && !context.IsMobileDevice)
        {
            series.Add(
                new ChartSeriesDefinition
                {
                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(context.Location, precipitation),
                    Aggregation = SeriesAggregationOptions.Sum,
                    BinGranularity = BinGranularities.ByYear,
                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                    SmoothingWindow = 20,
                    Value = SeriesValueOptions.Value,
                    Year = null,
                });
        }

        return new ChartState { Series = series };
    }
}
