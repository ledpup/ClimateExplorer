namespace ClimateExplorer.Web.Client.Shared.PopupContent;

using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;

public partial class HeatingScoreContent
{
    [Inject]
    public IDataService? DataService { get; set; }

    [Parameter]
    public Location? Location { get; set; }

    [PersistentState]
    public IEnumerable<HeatingScoreRow>? HeatingScoreTable { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (HeatingScoreTable is not null)
        {
            return;
        }

        HeatingScoreTable = await DataService!.GetHeatingScoreTable();
    }
}
