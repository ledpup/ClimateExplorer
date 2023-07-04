using Blazorise;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.Services;
using ClimateExplorer.Visualiser.Shared;
using ClimateExplorer.Visualiser.UiLogic;
using ClimateExplorer.Visualiser.UiModel;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ClimateExplorer.Visualiser.Pages;
public partial class RegionalAndGlobal : ChartablePage
{
    public RegionalAndGlobal()
    {
        pageName = "regionalandglobal";
    }
    
    Modal addDataSetModal { get; set; }

    void IDisposable.Dispose()
    {
        Dispose();
    }

    protected override async Task OnParametersSetAsync()
    {
        await UpdateUiStateBasedOnQueryString();
    }
    
    string GetPageTitle()
    {
        string title = $"ClimateExplorer";

        Logger.LogInformation("GetPageTitle() returning '" + title + "' NavigateTo");

        return title;
    }

    private Task ShowAddDataSetModal()
    {
        return addDataSetModal.Show();
    }

    protected override async Task UpdateComponents()
    {
    }
}
