namespace ClimateExplorer.Web.Client.Pages;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class RegionalAndGlobal : ChartablePage
{
    public RegionalAndGlobal()
    {
        PageName = "regionalandglobal";
    }

    protected override string PageTitle
    {
        get
        {
            return $"ClimateExplorer - Regional and global long-term climate trends";
        }
    }

    protected override string PageDescription
    {
        get
        {
            return "Explore regional and global long-term climate trends. View CO₂ levels, sea ice extent, sea level rise, and global temperature anomalies.";
        }
    }

    protected override string PageUrl
    {
        get
        {
            return $"https://climateexplorer.net/regionalandglobal";
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (Regions is null)
        {
            Regions = (await DataService!.GetRegions()).ToList();
            StateHasChanged();
        }

        if (await EnsureInitialChartStateAsync(location: null, CreateDefaultChartState))
        {
            StateHasChanged();
        }
    }

    private ChartState CreateDefaultChartState()
    {
        ArgumentNullException.ThrowIfNull(DataSetDefinitions, nameof(DataSetDefinitions));

        var co2 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
            DataSetDefinitions,
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
