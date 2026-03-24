namespace ClimateExplorer.Web.Client.Pages;

using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;

public partial class About
{
    private ApiMetadataModel? apiMetadata;

    [Inject]
    public IDataService? DataService { get; set; }

    protected string? Ogtitle { get; set; }

    protected string? Ogurl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Ogtitle = $"About ClimateExplorer";
        Ogurl = $"https://climateexplorer.net/about";

        if (DataService == null)
        {
            throw new NullReferenceException(nameof(DataService));
        }

        apiMetadata = await DataService.GetAbout();

        await base.OnInitializedAsync();
    }
}
