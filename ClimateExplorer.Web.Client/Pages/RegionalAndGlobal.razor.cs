using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.WebUtilities;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Web.Client.Pages;
public partial class RegionalAndGlobal : ChartablePage
{
    public RegionalAndGlobal()
    {
        pageName = "regionalandglobal";
    }

    bool finishedSetup;

    protected override string PageTitle
    {
        get
        {
            return $"Regional and global long-term climate trends"; ;
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

        if (firstRender)
        {
            Regions = (await DataService!.GetRegions()).ToList();
            GeographicalEntities = Regions;
        }

        if (!finishedSetup)
        {
            finishedSetup = true;
            var uri = NavManager!.ToAbsoluteUri(NavManager.Uri);
            var csd = QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
            if (csd)
            {
                await UpdateUiStateBasedOnQueryString(false);
            }
            else
            {
                await AddDefaultChart();
            }
            StateHasChanged();
        }
    }

    private async Task AddDefaultChart()
    {
        if (chartView == null || chartView.ChartSeriesList == null || chartView.ChartSeriesList.Any())
        {
            return;
        }

        var co2 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions!, Region.RegionId(Region.Atmosphere), DataType.CO2, null, throwIfNoMatch: true);

        chartView!.ChartSeriesList!.Add(
            new ChartSeriesDefinition()
            {
                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Atmosphere), co2!),
                Aggregation = SeriesAggregationOptions.Maximum,
                BinGranularity = BinGranularities.ByYear,
                SecondaryCalculation = SecondaryCalculationOptions.AnnualChange,
                Smoothing = SeriesSmoothingOptions.MovingAverage,
                SmoothingWindow = 10,
                Value = SeriesValueOptions.Value,
                Year = null,
                DisplayStyle = SeriesDisplayStyle.Line,
                RequestedColour = UiLogic.Colours.Brown,
            }
        );

        await BuildDataSets();
    }

    protected override async Task UpdateComponents()
    {
        await Task.CompletedTask;
    }
}
