namespace ClimateExplorer.Web.Client.Components.Location;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.UiModel;
using Microsoft.AspNetCore.Components;

public partial class LatestRecordsPanel
{
    private readonly LatestRecordsTabState temperatureState = new();
    private readonly LatestRecordsTabState precipitationState = new();
    private Guid? internalLocationId;

    [Parameter]
    public Location? Location { get; set; }

    [Inject]
    private ILatestRecordsService LatestRecordsService { get; set; } = default!;

    [Inject]
    private ILogger<LatestRecordsPanel> Logger { get; set; } = default!;

    private LatestRecordsTab ActiveTab { get; set; } = LatestRecordsTab.Temperature;
    private LatestRecordsTabState CurrentState => GetState(ActiveTab);
    private IReadOnlyList<LatestRecordTileViewModel> CurrentTiles => CurrentState.Result?.Tiles ?? [];
    private string CurrentEmptyMessage => CurrentState.Result?.EmptyMessage ?? "No latest records are available.";

    protected override async Task OnParametersSetAsync()
    {
        if (Location?.Id != internalLocationId)
        {
            internalLocationId = Location?.Id;
            temperatureState.Reset();
            precipitationState.Reset();
        }

        if (Location is not null)
        {
            await EnsureTabLoaded(ActiveTab);
        }
    }

    private async Task OnTabChanged(LatestRecordsTab tab)
    {
        ActiveTab = tab;
        await EnsureTabLoaded(tab);
    }

    private async Task RetryCurrentTab()
    {
        GetState(ActiveTab).Reset();
        await EnsureTabLoaded(ActiveTab);
    }

    private async Task EnsureTabLoaded(LatestRecordsTab tab)
    {
        if (Location is null)
        {
            return;
        }

        var state = GetState(tab);
        if (state.IsLoaded || state.IsLoading)
        {
            return;
        }

        state.IsLoading = true;
        state.ErrorMessage = null;

        try
        {
            state.Result = tab switch
            {
                LatestRecordsTab.Temperature => await LatestRecordsService.GetTemperatureRecords(Location.Id),
                LatestRecordsTab.Precipitation => await LatestRecordsService.GetPrecipitationRecords(Location.Id),
                _ => throw new NotImplementedException(),
            };
            state.IsLoaded = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to load latest {Tab} records for location {LocationId}", tab, Location.Id);
            state.ErrorMessage = $"Unable to load latest {tab.ToString().ToLowerInvariant()} records.";
        }
        finally
        {
            state.IsLoading = false;
        }
    }

    private LatestRecordsTabState GetState(LatestRecordsTab tab)
    {
        return tab switch
        {
            LatestRecordsTab.Temperature => temperatureState,
            LatestRecordsTab.Precipitation => precipitationState,
            _ => throw new NotImplementedException(),
        };
    }

    private sealed class LatestRecordsTabState
    {
        public bool IsLoading { get; set; }
        public bool IsLoaded { get; set; }
        public string? ErrorMessage { get; set; }
        public LatestRecordsTabResult? Result { get; set; }

        public void Reset()
        {
            IsLoading = false;
            IsLoaded = false;
            ErrorMessage = null;
            Result = null;
        }
    }
}
