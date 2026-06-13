namespace ClimateExplorer.Web.Client.Components.Location;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.UiModel;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationsPanel
{
    private readonly RecentObservationsTabState temperatureState = new();
    private readonly RecentObservationsTabState precipitationState = new();
    private Guid? internalLocationId;

    [Parameter]
    public Location? Location { get; set; }

    [Inject]
    private IRecentObservationsService RecentObservationsService { get; set; } = default!;

    [Inject]
    private ILogger<RecentObservationsPanel> Logger { get; set; } = default!;

    private RecentObservationsTab ActiveTab { get; set; } = RecentObservationsTab.Temperature;
    private RecentObservationsTabState CurrentState => GetState(ActiveTab);
    private IReadOnlyList<RecentObservationTileViewModel> CurrentTiles => CurrentState.Result?.Tiles ?? [];
    private string CurrentEmptyMessage => CurrentState.Result?.EmptyMessage ?? "No recent observations are available.";

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

    private async Task OnTabChanged(RecentObservationsTab tab)
    {
        ActiveTab = tab;
        await EnsureTabLoaded(tab);
    }

    private async Task RetryCurrentTab()
    {
        GetState(ActiveTab).Reset();
        await EnsureTabLoaded(ActiveTab);
    }

    private async Task EnsureTabLoaded(RecentObservationsTab tab)
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
                RecentObservationsTab.Temperature => await RecentObservationsService.GetTemperatureRecords(Location.Id),
                RecentObservationsTab.Precipitation => await RecentObservationsService.GetPrecipitationRecords(Location.Id),
                _ => throw new NotImplementedException(),
            };
            state.IsLoaded = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to load recent {Tab} observations for location {LocationId}", tab, Location.Id);
            state.ErrorMessage = $"Unable to load recent {tab.ToString().ToLowerInvariant()} observations.";
        }
        finally
        {
            state.IsLoading = false;
        }
    }

    private RecentObservationsTabState GetState(RecentObservationsTab tab)
    {
        return tab switch
        {
            RecentObservationsTab.Temperature => temperatureState,
            RecentObservationsTab.Precipitation => precipitationState,
            _ => throw new NotImplementedException(),
        };
    }

    private sealed class RecentObservationsTabState
    {
        public bool IsLoading { get; set; }
        public bool IsLoaded { get; set; }
        public string? ErrorMessage { get; set; }
        public RecentObservationsTabResult? Result { get; set; }

        public void Reset()
        {
            IsLoading = false;
            IsLoaded = false;
            ErrorMessage = null;
            Result = null;
        }
    }
}
