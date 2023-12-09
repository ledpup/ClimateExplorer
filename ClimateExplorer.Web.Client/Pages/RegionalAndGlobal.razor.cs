using ClimateExplorer.Core.DataPreparation;
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            Locations = (await DataService!.GetLocations()).ToList();   
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

        var co2 = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions!, null, DataType.CO2, null, throwIfNoMatch: true);

        chartView!.ChartSeriesList!.Add(
            new ChartSeriesDefinition()
            {
                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(null, co2!),
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

    string GetPageTitle()
    {
        string title = $"ClimateExplorer";

        Logger!.LogInformation("GetPageTitle() returning '" + title + "' NavigateTo");

        return title;
    }

    protected override async Task UpdateComponents()
    {
        await Task.CompletedTask;
    }
}
