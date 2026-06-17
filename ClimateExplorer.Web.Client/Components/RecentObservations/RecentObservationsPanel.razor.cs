namespace ClimateExplorer.Web.Client.Components.Location;

using System.Globalization;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.UiModel;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationsPanel
{
    private readonly RecentObservationsTabState temperatureState = new();
    private readonly RecentObservationsTabState precipitationState = new();
    private readonly RecentObservationPeriodSelection periodSelection = new();
    private float completenessThreshold = RecentObservationCompletenessThreshold.Default;
    private Guid? internalLocationId;

    [Parameter]
    public Location? Location { get; set; }

    [Inject]
    private IRecentObservationsService RecentObservationsService { get; set; } = default!;

    [Inject]
    private ILogger<RecentObservationsPanel> Logger { get; set; } = default!;

    private RecentObservationsTab ActiveTab { get; set; } = RecentObservationsTab.Temperature;
    private RecentObservationsTabState CurrentState => GetState(ActiveTab);
    private IReadOnlyList<RecentObservationTileViewModel> CurrentTiles => CurrentState.Result?
        .ApplyCompletenessThreshold(completenessThreshold)
        .Tiles
        .Where(IsVisibleTile)
        .ToList() ?? [];
    private int CompletenessThresholdPercent => RecentObservationCompletenessThreshold.ToPercentage(completenessThreshold);
    private IEnumerable<RecentObservationTileViewModel> TilesBeforeMonthControls => CurrentTiles.Where(IsBeforeMonthControls);
    private IEnumerable<RecentObservationTileViewModel> SeasonTiles => CurrentTiles.Where(IsSeasonTile);
    private IEnumerable<RecentObservationTileViewModel> TilesAfterSeasonControls => CurrentTiles.Where(IsAfterSeasonControls);
    private string CurrentEmptyMessage => CurrentState.Result?.EmptyMessage ?? "No recent observations are available.";
    private string AddDayButtonLabel => CreateAddButtonLabel(RecentObservationPeriodKind.Daily, "day");
    private string AddMonthButtonLabel => CreateAddButtonLabel(RecentObservationPeriodKind.PreviousMonth, "month");
    private string AddSeasonButtonLabel => CreateAddButtonLabel(RecentObservationPeriodKind.PreviousSeason, "season");
    private bool IsAddEarlierDayDisabled => !periodSelection.CanAddEarlierDay(GetAvailableOffsets(RecentObservationPeriodKind.Daily));
    private bool IsAddEarlierMonthDisabled => !periodSelection.CanAddEarlierMonth(GetAvailableOffsets(RecentObservationPeriodKind.PreviousMonth));
    private bool IsAddEarlierSeasonDisabled => !periodSelection.CanAddEarlierSeason(GetAvailableOffsets(RecentObservationPeriodKind.PreviousSeason));

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
                    RecentObservationPeriodSelection.MaximumPreviousDayCount,
                    RecentObservationPeriodSelection.MaximumPreviousMonthCount,
                    RecentObservationPeriodSelection.MaximumPreviousSeasonCount),
                RecentObservationsTab.Precipitation => await RecentObservationsService.GetPrecipitationRecords(
                    Location,
                    RecentObservationPeriodSelection.MaximumPreviousDayCount,
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

    private void AddEarlierDay()
    {
        periodSelection.AddEarlierDay(GetAvailableOffsets(RecentObservationPeriodKind.Daily));
    }

    private void AddEarlierMonth()
    {
        periodSelection.AddEarlierMonth(GetAvailableOffsets(RecentObservationPeriodKind.PreviousMonth));
    }

    private void AddEarlierSeason()
    {
        periodSelection.AddEarlierSeason(GetAvailableOffsets(RecentObservationPeriodKind.PreviousSeason));
    }

    private void RemoveTile(RecentObservationTileViewModel tile)
    {
        periodSelection.Remove(tile);
    }

    private void OnCompletenessThresholdChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var thresholdPercent))
        {
            return;
        }

        completenessThreshold = RecentObservationCompletenessThreshold.FromPercentage(thresholdPercent);
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
            RecentObservationPeriodKind.LatestSevenDays or
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
        return periodSelection.IsVisible(tile);
    }

    private bool IsRemovableTile(RecentObservationTileViewModel tile)
    {
        return periodSelection.IsRemovable(tile);
    }

    private string CreateRemoveTileLabel(RecentObservationTileViewModel tile)
    {
        return $"Remove {tile.PeriodTitle}";
    }

    private string CreateAddButtonLabel(RecentObservationPeriodKind periodKind, string fallbackPeriodName)
    {
        return periodSelection.CreateAddButtonLabel(periodKind, GetAvailableTiles(periodKind), fallbackPeriodName);
    }

    private string GetTileKey(RecentObservationTileViewModel tile)
    {
        return $"{tile.PeriodKind}:{tile.PeriodStartDate:yyyy-MM-dd}:{tile.PeriodEndDate:yyyy-MM-dd}:{tile.PeriodTitle}";
    }

    private IEnumerable<int> GetAvailableOffsets(RecentObservationPeriodKind periodKind)
    {
        return GetAvailableTiles(periodKind)
            .Select(tile => tile.PeriodOffset!.Value)
            .Order();
    }

    private IEnumerable<RecentObservationTileViewModel> GetAvailableTiles(RecentObservationPeriodKind periodKind)
    {
        if (CurrentState.Result is null)
        {
            return [];
        }

        return CurrentState.Result.Tiles
            .Where(tile => tile.PeriodKind == periodKind && tile.PeriodOffset.HasValue)
            .OrderBy(tile => tile.PeriodOffset!.Value);
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
