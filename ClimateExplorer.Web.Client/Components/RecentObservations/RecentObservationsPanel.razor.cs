namespace ClimateExplorer.Web.Client.Components.Location;

using System.Globalization;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using Microsoft.AspNetCore.Components;

public partial class RecentObservationsPanel
{
    private readonly RecentObservationsTabState temperatureState = new();
    private readonly RecentObservationsTabState precipitationState = new();
    private readonly RecentObservationPeriodSelection periodSelection = new();
    private float completenessThreshold = RecentObservationCompletenessThreshold.Default;
    private Guid? internalLocationId;
    private DateOnly? selectedReferenceDate;
    private string referenceDateInputValue = string.Empty;
    private string? referenceDateValidationMessage;
    private ComparisonEndMode selectedComparisonEndMode = ComparisonEndMode.FullDataset;

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
    private string AddYearButtonLabel => CreateAddButtonLabel(RecentObservationPeriodKind.PreviousYear, "year");
    private string ExpandCollapseAllLabel => CurrentState.ExpansionStates.CreateToggleAllLabel(CurrentExpansionTargets);
    private bool HasExpandableCurrentTiles => CurrentState.ExpansionStates.HasExpandableTile(CurrentExpansionTargets);
    private bool AreAllExpandableCurrentTilesExpanded => CurrentState.ExpansionStates.AreAllExpandableTilesExpanded(CurrentExpansionTargets);
    private IEnumerable<RecentObservationTileExpansionTarget> CurrentExpansionTargets =>
        CurrentTiles.Select(tile => new RecentObservationTileExpansionTarget(GetTileKey(tile), IsExpandableTile(tile)));
    private string ReferenceDateInputId => $"recent-observations-reference-date-{ActiveTab.ToString().ToLowerInvariant()}";
    private string ReferenceDateHelpId => $"{ReferenceDateInputId}-help";
    private string ReferenceDateInputValidationClass => string.IsNullOrWhiteSpace(referenceDateValidationMessage) ? string.Empty : "is-invalid";
    private string ComparisonRangeInputId => $"recent-observations-comparison-range-{ActiveTab.ToString().ToLowerInvariant()}";
    private bool IsResetReferenceDateDisabled => CurrentState.Result?.ReferenceDate == CurrentState.Result?.MaximumReferenceDate;
    private bool IsAddEarlierDayDisabled => !periodSelection.CanAddEarlierDay(GetAvailableOffsets(RecentObservationPeriodKind.Daily));
    private bool IsAddEarlierMonthDisabled => !periodSelection.CanAddEarlierMonth(GetAvailableOffsets(RecentObservationPeriodKind.PreviousMonth));
    private bool IsAddEarlierSeasonDisabled => !periodSelection.CanAddEarlierSeason(GetAvailableOffsets(RecentObservationPeriodKind.PreviousSeason));
    private bool IsAddEarlierYearDisabled => !periodSelection.CanAddEarlierYear(GetAvailableOffsets(RecentObservationPeriodKind.PreviousYear));
    private IReadOnlyList<RecentObservationSourceMetadata> CurrentSourceMetadata => CurrentState.Result?.SourceMetadata ?? [];
    private RecentObservationSourceMetadata? ObservationSourceMetadata => CurrentSourceMetadata
        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.SourceName) && !string.IsNullOrWhiteSpace(x.StationId));
    private IReadOnlyList<RecentObservationSourceMetadata> CurrentRetrievalMetadata =>
        RecentObservationRetrievalMetadataSelector.Select(CurrentSourceMetadata);

    protected override async Task OnParametersSetAsync()
    {
        if (Location?.Id != internalLocationId)
        {
            internalLocationId = Location?.Id;
            periodSelection.Reset();
            selectedReferenceDate = null;
            referenceDateInputValue = string.Empty;
            referenceDateValidationMessage = null;
            selectedComparisonEndMode = ComparisonEndMode.FullDataset;
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
        if (state.IsLoading)
        {
            return;
        }

        if (state.DataSet is not null)
        {
            RecalculateTab(tab, updateSelectedReferenceDate: tab == ActiveTab);
            return;
        }

        state.IsLoading = true;
        state.ErrorMessage = null;

        try
        {
            state.DataSet = tab switch
            {
                RecentObservationsTab.Temperature => await RecentObservationsService.LoadTemperatureData(Location),
                RecentObservationsTab.Precipitation => await RecentObservationsService.LoadPrecipitationData(Location),
                _ => throw new NotImplementedException(),
            };
            RecalculateTab(tab, updateSelectedReferenceDate: tab == ActiveTab);
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

    private void AddEarlierYear()
    {
        periodSelection.AddEarlierYear(GetAvailableOffsets(RecentObservationPeriodKind.PreviousYear));
    }

    private void RemoveTile(RecentObservationTileViewModel tile)
    {
        periodSelection.Remove(tile);
    }

    private void ToggleAllTileExpansion()
    {
        CurrentState.ExpansionStates.ToggleAll(CurrentExpansionTargets);
    }

    private void OnTileExpansionChanged()
    {
    }

    private void OnCompletenessThresholdChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var thresholdPercent))
        {
            return;
        }

        completenessThreshold = RecentObservationCompletenessThreshold.FromPercentage(thresholdPercent);
    }

    private void OnReferenceDateInput(ChangeEventArgs e)
    {
        referenceDateInputValue = e.Value?.ToString() ?? string.Empty;
        referenceDateValidationMessage = null;
    }

    private async Task OnReferenceDateChanged(ChangeEventArgs e)
    {
        referenceDateInputValue = e.Value?.ToString() ?? string.Empty;

        if (!TryValidateReferenceDateInput(referenceDateInputValue, out var referenceDate))
        {
            return;
        }

        selectedReferenceDate = referenceDate;
        periodSelection.Reset();
        RecalculateLoadedTabs();

        await EnsureTabLoaded(ActiveTab);
    }

    private async Task ResetReferenceDate()
    {
        selectedReferenceDate = null;
        referenceDateValidationMessage = null;
        periodSelection.Reset();
        RecalculateLoadedTabs();

        await EnsureTabLoaded(ActiveTab);
    }

    private bool TryValidateReferenceDateInput(string input, out DateOnly referenceDate)
    {
        if (!DateOnly.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out referenceDate))
        {
            referenceDateValidationMessage = "Enter a date in YYYY-MM-DD format.";
            return false;
        }

        var result = CurrentState.Result;
        if (result?.MinimumReferenceDate is { } minimumReferenceDate && referenceDate < minimumReferenceDate)
        {
            referenceDateValidationMessage = $"Enter a date on or after {FormatDateInput(minimumReferenceDate)}.";
            return false;
        }

        if (result?.MaximumReferenceDate is { } maximumReferenceDate && referenceDate > maximumReferenceDate)
        {
            referenceDateValidationMessage = $"Enter a date on or before {FormatDateInput(maximumReferenceDate)}.";
            return false;
        }

        referenceDateValidationMessage = null;
        return true;
    }

    private async Task OnComparisonEndModeChanged(ComparisonEndMode comparisonEndMode)
    {
        selectedComparisonEndMode = comparisonEndMode;
        RecalculateLoadedTabs();

        await EnsureTabLoaded(ActiveTab);
    }

    private void RecalculateLoadedTabs()
    {
        RecalculateTab(RecentObservationsTab.Temperature, updateSelectedReferenceDate: ActiveTab == RecentObservationsTab.Temperature);
        RecalculateTab(RecentObservationsTab.Precipitation, updateSelectedReferenceDate: ActiveTab == RecentObservationsTab.Precipitation);
    }

    private void RecalculateTab(RecentObservationsTab tab, bool updateSelectedReferenceDate)
    {
        if (Location is null)
        {
            return;
        }

        var state = GetState(tab);
        if (state.DataSet is null)
        {
            return;
        }

        state.Result = RecentObservationsService.Calculate(Location, state.DataSet, CreateOptions());
        if (updateSelectedReferenceDate && state.Result.ReferenceDate.HasValue)
        {
            selectedReferenceDate = state.Result.ReferenceDate;
            referenceDateInputValue = FormatDateInput(state.Result.ReferenceDate);
            referenceDateValidationMessage = null;
        }

        state.IsLoaded = true;
    }

    private RecentObservationsOptions CreateOptions()
    {
        return new RecentObservationsOptions
        {
            ReferenceDate = selectedReferenceDate,
            ComparisonEndMode = selectedComparisonEndMode,
            CompletenessThreshold = completenessThreshold,
            PreviousDayCount = RecentObservationPeriodSelection.MaximumPreviousDayCount,
            PreviousMonthCount = RecentObservationPeriodSelection.MaximumPreviousMonthCount,
            PreviousSeasonCount = RecentObservationPeriodSelection.MaximumPreviousSeasonCount,
            PreviousYearCount = RecentObservationPeriodSelection.MaximumPreviousYearCount,
        };
    }

    private string FormatDateInput(DateOnly? date)
    {
        return date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private string FormatDateLong(DateOnly? date)
    {
        return date?.ToString("yyyy MMMM dd", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private string FormatUtcTimestamp(DateTimeOffset? timestamp)
    {
        return timestamp?.ToUniversalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private string FormatSourceUrlLabel(RecentObservationSourceMetadata metadata)
    {
        return string.IsNullOrWhiteSpace(metadata.SourceUrlLabel)
            ? metadata.SourceUrl ?? string.Empty
            : metadata.SourceUrlLabel;
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

    private RecentObservationTileExpansionState GetTileExpansionState(RecentObservationTileViewModel tile)
    {
        return CurrentState.ExpansionStates.GetOrAdd(GetTileKey(tile));
    }

    private bool IsExpandableTile(RecentObservationTileViewModel tile)
    {
        return tile.AvailableExpandedTabs.Count > 0;
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
        public RecentObservationsDataSet? DataSet { get; set; }
        public RecentObservationsTabResult? Result { get; set; }
        public RecentObservationTileExpansionStateCollection ExpansionStates { get; private set; } = new();

        public void Reset()
        {
            IsLoading = false;
            IsLoaded = false;
            ErrorMessage = null;
            DataSet = null;
            Result = null;
            ExpansionStates = new RecentObservationTileExpansionStateCollection();
        }
    }
}
