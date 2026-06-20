namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

public sealed class DefaultChartProvider : IDefaultChartProvider
{
    public ChartState CreateDefault(ChartPageContext context)
    {
        return context.PageKind switch
        {
            ChartPageKind.Location => CreateLocationDefault(context),
            ChartPageKind.RegionalAndGlobal => CreateRegionalAndGlobalDefault(context),
            _ => throw new NotImplementedException($"Chart page kind {context.PageKind}"),
        };
    }

    private static ChartState CreateLocationDefault(ChartPageContext context)
    {
        if (context.Location is null)
        {
            throw new InvalidOperationException("A location page context must include a location before creating default charts.");
        }

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

    private static ChartState CreateRegionalAndGlobalDefault(ChartPageContext context)
    {
        var co2 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
            context.DataSetDefinitions,
            Region.RegionId(Region.Atmosphere),
            DataType.CO2,
            null,
            throwIfNoMatch: true);

        return new ChartState
        {
            Series =
            [
                new ChartSeriesDefinition
                {
                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Atmosphere), co2!),
                    Aggregation = SeriesAggregationOptions.Mean,
                    BinGranularity = BinGranularities.ByYear,
                    SecondaryCalculation = SecondaryCalculationOptions.AnnualChange,
                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                    SmoothingWindow = 10,
                    Value = SeriesValueOptions.Value,
                    Year = null,
                    DisplayStyle = SeriesDisplayStyle.Line,
                    RequestedColour = Colours.Brown,
                },
            ],
        };
    }
}
