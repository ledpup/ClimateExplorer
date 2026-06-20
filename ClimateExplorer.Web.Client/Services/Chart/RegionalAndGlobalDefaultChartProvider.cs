namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

public sealed class RegionalAndGlobalDefaultChartProvider : IRegionalAndGlobalDefaultChartProvider
{
    public ChartState CreateDefault(RegionalAndGlobalDefaultChartContext context)
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
