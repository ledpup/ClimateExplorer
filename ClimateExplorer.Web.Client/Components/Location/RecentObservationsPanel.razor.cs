namespace ClimateExplorer.Web.Client.Components.Location;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.UiModel;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationsPanel
{
    private readonly RecentObservationsTabState temperatureState = new();
    private readonly RecentObservationsTabState precipitationState = new();
    private readonly RecentObservationPeriodSelection periodSelection = new();
    private Guid? internalLocationId;

    [Parameter]
    public Location? Location { get; set; }

    [Inject]
    private IRecentObservationsService RecentObservationsService { get; set; } = default!;

    [Inject]
    private ILogger<RecentObservationsPanel> Logger { get; set; } = default!;

    private RecentObservationsTab ActiveTab { get; set; } = RecentObservationsTab.Temperature;
    private RecentObservationsTabState CurrentState => GetState(ActiveTab);
    private IReadOnlyList<RecentObservationTileViewModel> CurrentTiles => CurrentState.Result?.Tiles.Where(IsVisibleTile).ToList() ?? [];
    private IEnumerable<RecentObservationTileViewModel> TilesBeforeMonthControls => CurrentTiles.Where(IsBeforeMonthControls);
    private IEnumerable<RecentObservationTileViewModel> SeasonTiles => CurrentTiles.Where(IsSeasonTile);
    private IEnumerable<RecentObservationTileViewModel> TilesAfterSeasonControls => CurrentTiles.Where(IsAfterSeasonControls);
    private string CurrentEmptyMessage => CurrentState.Result?.EmptyMessage ?? "No recent observations are available.";
    private bool IsAddEarlierMonthDisabled => periodSelection.IsAddEarlierMonthDisabled;
    private bool IsRemoveMonthDisabled => periodSelection.IsRemoveMonthDisabled;
    private bool IsAddEarlierSeasonDisabled => periodSelection.IsAddEarlierSeasonDisabled;
    private bool IsRemoveSeasonDisabled => periodSelection.IsRemoveSeasonDisabled;

    protected override async Task OnParametersSetAsync()
    {
        if (Location?.Id != internalLocationId)
        {
            internalLocationId = Location?.Id;
            periodSelection.Reset();
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
                RecentObservationsTab.Temperature => await RecentObservationsService.GetTemperatureRecords(
                    Location,
                    RecentObservationPeriodSelection.MaximumPreviousMonthCount,
                    RecentObservationPeriodSelection.MaximumPreviousSeasonCount),
                RecentObservationsTab.Precipitation => await RecentObservationsService.GetPrecipitationRecords(
                    Location,
                    RecentObservationPeriodSelection.MaximumPreviousMonthCount,
                    RecentObservationPeriodSelection.MaximumPreviousSeasonCount),
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

    private void AddEarlierMonth()
    {
        periodSelection.AddEarlierMonth();
    }

    private void RemoveMonth()
    {
        periodSelection.RemoveMonth();
    }

    private void AddEarlierSeason()
    {
        periodSelection.AddEarlierSeason();
    }

    private void RemoveSeason()
    {
        periodSelection.RemoveSeason();
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

    private bool IsBeforeMonthControls(RecentObservationTileViewModel tile)
    {
        return tile.PeriodKind is
            RecentObservationPeriodKind.Daily or
            RecentObservationPeriodKind.LastWeek or
            RecentObservationPeriodKind.CurrentMonth or
            RecentObservationPeriodKind.PreviousMonth;
    }

    private bool IsSeasonTile(RecentObservationTileViewModel tile)
    {
        return tile.PeriodKind is RecentObservationPeriodKind.CurrentSeason or RecentObservationPeriodKind.PreviousSeason;
    }

    private bool IsAfterSeasonControls(RecentObservationTileViewModel tile)
    {
        return !IsBeforeMonthControls(tile) && !IsSeasonTile(tile);
    }

    private bool IsVisibleTile(RecentObservationTileViewModel tile)
    {
        return tile.PeriodKind switch
        {
            RecentObservationPeriodKind.PreviousMonth => tile.PeriodOffset <= periodSelection.PreviousMonthCount,
            RecentObservationPeriodKind.PreviousSeason => tile.PeriodOffset <= periodSelection.PreviousSeasonCount,
            _ => true,
        };
    }

    private string GetTileKey(RecentObservationTileViewModel tile)
    {
        return $"{tile.PeriodKind}:{tile.PeriodTitle}";
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
