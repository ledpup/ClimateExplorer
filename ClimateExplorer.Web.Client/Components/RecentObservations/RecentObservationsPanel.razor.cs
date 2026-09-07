#pragma warning disable SA1204
namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using System.Globalization;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

public partial class RecentObservationsPanel
{
    private const int BulkAddMonthCount = 12;
    private const int BulkAddSeasonCount = 4;

    private readonly Dictionary<string, RecentObservationsTabState> tabStates = [];
    private readonly RecentObservationPeriodSelection periodSelection = new();
    private float completenessThreshold = RecentObservationCompletenessThreshold.Default;
    private Guid? internalContextId;
    private DateOnly? selectedReferenceDate;
    private string referenceDateInputValue = string.Empty;
    private string? referenceDateValidationMessage;
    private ComparisonEndMode selectedComparisonEndMode = ComparisonEndMode.FullDataset;
    private DataAdjustment? selectedDataAdjustment = DataAdjustment.Adjusted;
    private bool synchroniseTabs = true;

    [Parameter]
    public RecentObservationsContext? Context { get; set; }

    [Parameter]
    public Location? Location { get; set; }

    [Parameter]
    public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    [Inject]
    private IRecentObservationsService RecentObservationsService { get; set; } = default!;

    [Inject]
    private ILogger<RecentObservationsPanel> Logger { get; set; } = default!;

    [Inject]
    private IExporter Exporter { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private NavigationManager NavManager { get; set; } = default!;

    private string? ActiveTabKey { get; set; }
    private ObservationDomain? ActiveDomain => Context?.Domains.FirstOrDefault(x => x.Key == ActiveTabKey) ?? Context?.Domains.FirstOrDefault();
    private RecentObservationsTabState CurrentState => ActiveDomain is null ? new RecentObservationsTabState() : GetState(ActiveDomain.Key);
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
    private string AddMonthButtonLabel => CreateAddButtonLabel(RecentObservationPeriodKind.Month, "month");
    private string AddSeasonButtonLabel => CreateAddButtonLabel(RecentObservationPeriodKind.Season, "season");
    private string AddYearButtonLabel => CreateAddButtonLabel(RecentObservationPeriodKind.Year, "year");
    private string AddMonthBulkAriaLabel => $"Add {BulkAddMonthCount} earlier months";
    private string AddSeasonBulkAriaLabel => $"Add {BulkAddSeasonCount} earlier seasons";
    private string ExpandCollapseAllLabel => CurrentState.ExpansionStates.CreateToggleAllLabel(CurrentExpansionTargets);
    private bool HasExpandableCurrentTiles => CurrentState.ExpansionStates.HasExpandableTile(CurrentExpansionTargets);
    private bool AreAllExpandableCurrentTilesExpanded => CurrentState.ExpansionStates.AreAllExpandableTilesExpanded(CurrentExpansionTargets);
    private IEnumerable<RecentObservationTileExpansionTarget> CurrentExpansionTargets =>
        CurrentTiles.Select(tile => new RecentObservationTileExpansionTarget(GetTileKey(tile), IsExpandableTile(tile)));
    private string ReferenceDateInputId => $"recent-observations-reference-date-{ActiveDomain?.Key ?? "none"}";
    private string ReferenceDateHelpId => $"{ReferenceDateInputId}-help";
    private string ReferenceDateInputValidationClass => string.IsNullOrWhiteSpace(referenceDateValidationMessage) ? string.Empty : "is-invalid";
    private string ComparisonRangeInputId => $"recent-observations-comparison-range-{ActiveDomain?.Key ?? "none"}";
    private bool IsResetReferenceDateDisabled => CurrentState.Result?.ReferenceDate == CurrentState.Result?.MaximumReferenceDate;
    private bool IsAddEarlierDayDisabled => !periodSelection.CanAddEarlierDay(GetAvailableOffsets(RecentObservationPeriodKind.Daily));
    private bool IsAddEarlierMonthDisabled => !periodSelection.CanAddEarlierMonth(GetAvailableOffsets(RecentObservationPeriodKind.Month));
    private bool IsAddEarlierSeasonDisabled => !periodSelection.CanAddEarlierSeason(GetAvailableOffsets(RecentObservationPeriodKind.Season));
    private bool IsAddEarlierYearDisabled => !periodSelection.CanAddEarlierYear(GetAvailableOffsets(RecentObservationPeriodKind.Year));
    private DataAdjustment? SelectedDataAdjustment => selectedDataAdjustment;
    private List<DataAdjustment?> AvailableDataAdjustments { get; set; } = [];

    protected override async Task OnParametersSetAsync()
    {
        if (Context?.Id != internalContextId)
        {
            internalContextId = Context?.Id;
            periodSelection.Reset();
            selectedReferenceDate = null;
            referenceDateInputValue = string.Empty;
            referenceDateValidationMessage = null;
            selectedComparisonEndMode = ComparisonEndMode.FullDataset;
            tabStates.Clear();
            ActiveTabKey = Context?.Domains.FirstOrDefault()?.Key;
            UpdateAvailableDataAdjustments();
        }

        if (Context is not null && ActiveDomain is not null && GetState(ActiveDomain.Key).DataSet is null)
        {
            await EnsureTabLoaded(ActiveDomain);
        }
    }

    private async Task OnTabChanged(string domainKey)
    {
        var domain = Context?.Domains.FirstOrDefault(x => x.Key == domainKey);
        if (domain is null)
        {
            return;
        }

        ActiveTabKey = domainKey;
        await EnsureTabLoaded(domain);
    }

    private void UpdateAvailableDataAdjustments()
    {
        var domain = ActiveDomain;
        if (Context is null || domain is null || !domain.SupportsAdjustment || DataSetDefinitions is null)
        {
            AvailableDataAdjustments = [];
            selectedDataAdjustment = DataAdjustment.Adjusted;
            return;
        }

        AvailableDataAdjustments = [.. DataSetDefinitions
            .Where(x => x.LocationIds != null && x.LocationIds.Contains(Context.Id))
            .SelectMany(x => x.MeasurementDefinitions ?? [])
            .Where(x => domain.DataTypeRequests.Contains(x.DataType))
            .Where(x => x.DataResolution == DataResolution.Daily)
            .Select(x => x.DataAdjustment)
            .Distinct()];

        selectedDataAdjustment = AvailableDataAdjustments.Contains(DataAdjustment.Adjusted)
            ? DataAdjustment.Adjusted
            : AvailableDataAdjustments.FirstOrDefault();
    }

    private async Task OnAdjustedChanged(bool value)
    {
        var domain = ActiveDomain;
        if (domain is null)
        {
            return;
        }

        selectedDataAdjustment = value
            ? DataAdjustment.Adjusted
            : AvailableDataAdjustments.FirstOrDefault(x => x != DataAdjustment.Adjusted);

        GetState(domain.Key).Reset();
        await EnsureTabLoaded(domain);
    }

    private async Task RetryCurrentTab()
    {
        var domain = ActiveDomain;
        if (domain is null)
        {
            return;
        }

        GetState(domain.Key).Reset();
        await EnsureTabLoaded(domain);
    }

    private async Task EnsureTabLoaded(ObservationDomain domain)
    {
        if (Context is null)
        {
            return;
        }

        var state = GetState(domain.Key);
        if (state.IsLoading)
        {
            return;
        }

        if (state.DataSet is not null)
        {
            RecalculateTab(domain, updateSelectedReferenceDate: domain.Key == ActiveDomain?.Key);
            return;
        }

        state.IsLoading = true;
        state.ErrorMessage = null;

        try
        {
            state.DataSet = await RecentObservationsService.LoadData(Context.Id, domain, domain.SupportsAdjustment ? SelectedDataAdjustment : null);
            RecalculateTab(domain, updateSelectedReferenceDate: domain.Key == ActiveDomain?.Key);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to load recent {Domain} observations for {ContextId}", domain.Key, Context.Id);
            state.ErrorMessage = $"Unable to load recent {domain.TabLabel.ToLowerInvariant()} observations.";
        }
        finally
        {
            state.IsLoading = false;
        }
    }

    private void AddEarlierDay()
    {
        periodSelection.AddEarlierDay(GetAvailableOffsets(RecentObservationPeriodKind.Daily));

        // periodSelection is shared across every tab, and growing its visible count also moves
        // the lookahead buffer CreateOptions() requests, so every loaded tab needs recalculating.
        RecalculateLoadedTabs();
    }

    private void AddEarlierMonth()
    {
        periodSelection.AddEarlierMonth(GetAvailableOffsets(RecentObservationPeriodKind.Month));
        RecalculateLoadedTabs();
    }

    private void AddEarlierSeason()
    {
        periodSelection.AddEarlierSeason(GetAvailableOffsets(RecentObservationPeriodKind.Season));
        RecalculateLoadedTabs();
    }

    private void AddEarlierYear()
    {
        periodSelection.AddEarlierYear(GetAvailableOffsets(RecentObservationPeriodKind.Year));
        RecalculateLoadedTabs();
    }

    private Task AddEarlierMonthsBulk()
    {
        return RunBulkAddWithLoadingIndicator(() =>
        {
            ClearCurrentTilesUnlessAllMatch(RecentObservationPeriodKind.Month);
            AddBulkTiles(RecentObservationPeriodKind.Month, BulkAddMonthCount, AddEarlierMonth, () => IsAddEarlierMonthDisabled);
        });
    }

    private Task AddEarlierSeasonsBulk()
    {
        return RunBulkAddWithLoadingIndicator(() =>
        {
            ClearCurrentTilesUnlessAllMatch(RecentObservationPeriodKind.Season);
            AddBulkTiles(RecentObservationPeriodKind.Season, BulkAddSeasonCount, AddEarlierSeason, () => IsAddEarlierSeasonDisabled);
        });
    }

    // The tile recalculations AddBulkTiles triggers run synchronously and can take a noticeable
    // while, so this shows the tab's loading message first and yields once (StateHasChanged alone
    // only queues the render; the yield lets the renderer actually flush it to the DOM before the
    // synchronous work blocks the UI thread) before running the bulk add.
    private async Task RunBulkAddWithLoadingIndicator(Action bulkAdd)
    {
        var state = CurrentState;
        state.IsLoading = true;
        StateHasChanged();
        await Task.Yield();

        try
        {
            bulkAdd();
        }
        finally
        {
            state.IsLoading = false;
        }
    }

    // Clears every currently visible tile on the active tab, unless every one of them already
    // belongs to the bulk button's own family - a literal "remove all tiles if they're not already
    // all month/season tiles". The to-date tile (offset 0) gets removed along with everything else
    // here; AddBulkTiles is what puts it straight back as the anchor for the bulk-add that follows.
    private void ClearCurrentTilesUnlessAllMatch(RecentObservationPeriodKind allowedKind)
    {
        if (CurrentTiles.All(tile => tile.PeriodKind == allowedKind))
        {
            return;
        }

        foreach (var tile in CurrentTiles)
        {
            RemoveTile(tile);
        }
    }

    // "Add 12 months"/"Add 4 seasons" always anchors on the current month/season, not on wherever
    // AddEarlierMonth/AddEarlierSeason's offset cursor happens to be - so it re-shows the offset-0
    // "to date" tile first (undoing a removal from either the clear step above or an earlier
    // manual remove-tile click) and only asks for the remaining count from the earlier-period
    // loop. When "to date" isn't meaningful for this reference date (e.g. the 1st of the month, or
    // the first month of a season - this family has no offset-0 tile in the current calculation at
    // all), there's nothing to re-show, and RecalculateTab's EnsureDefaults has already seeded the
    // offset-1 tile as the stand-in anchor instead - so the full count is asked for from the loop,
    // which picks that seeded offset up as its first addition.
    private void AddBulkTiles(RecentObservationPeriodKind family, int totalCount, Action addEarlierOne, Func<bool> isAddEarlierDisabled)
    {
        var hasCurrentPeriod = CurrentState.Result?.Tiles.Any(tile => tile.PeriodKind == family && tile.PeriodOffset == 0) == true;
        if (hasCurrentPeriod)
        {
            periodSelection.EnsureVisible(family);
        }

        var remainingCount = hasCurrentPeriod ? totalCount - 1 : totalCount;
        for (var i = 0; i < remainingCount && !isAddEarlierDisabled(); i++)
        {
            addEarlierOne();
        }
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

    private void OnTileGroupSelected(MetricGroupKey? key)
    {
        if (!synchroniseTabs)
        {
            return;
        }

        foreach (var tile in CurrentTiles)
        {
            CurrentState.ExpansionStates.GetOrAdd(GetTileKey(tile)).SelectGroup(key);
        }
    }

    private async Task OnTrendDownloadRequested(TrendDownloadRequest request)
    {
        if (Location is null)
        {
            return;
        }

        var fileStream = Exporter.ExportTrendData(Logger, Location, request.DataTypeLabel, request.WindowLabel, request.Points, NavManager.Uri);
        var fileName = $"{Location.Name}-{request.DataTypeLabel}-{request.WindowLabel}-trend-data.csv";

        using var streamRef = new DotNetStreamReference(stream: fileStream);
        await JsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
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

        if (ActiveDomain is not null)
        {
            await EnsureTabLoaded(ActiveDomain);
        }
    }

    private async Task ResetReferenceDate()
    {
        selectedReferenceDate = null;
        referenceDateValidationMessage = null;
        periodSelection.Reset();
        RecalculateLoadedTabs();

        if (ActiveDomain is not null)
        {
            await EnsureTabLoaded(ActiveDomain);
        }
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

        if (ActiveDomain is not null)
        {
            await EnsureTabLoaded(ActiveDomain);
        }
    }

    private void RecalculateLoadedTabs()
    {
        if (Context is null)
        {
            return;
        }

        foreach (var domain in Context.Domains)
        {
            RecalculateTab(domain, updateSelectedReferenceDate: domain.Key == ActiveDomain?.Key);
        }
    }

    private void RecalculateTab(ObservationDomain domain, bool updateSelectedReferenceDate)
    {
        if (Context is null)
        {
            return;
        }

        var state = GetState(domain.Key);
        if (state.DataSet is null)
        {
            return;
        }

        state.Result = RecentObservationsService.Calculate(Context.Latitude, state.DataSet, CreateOptions());
        if (periodSelection.EnsureDefaults(state.Result.Tiles))
        {
            // Seeding a default tile (e.g. viewing on day 1 of a month/season/year, before the
            // "to date" tile is meaningful) grows one of the visible counts, so recalculate to
            // refresh the "Add earlier" lookahead buffer for the newly-visible tile.
            state.Result = RecentObservationsService.Calculate(Context.Latitude, state.DataSet, CreateOptions());
        }

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
        // Request only what's currently visible plus a one-tile lookahead buffer per period kind
        // (rather than the unbounded RecentObservationsOptions defaults), so we're not computing
        // metrics and historical rankings for every day/month/season/year a station has ever
        // recorded. The buffer lets GetAvailableTiles/GetNextAddTile preview the next "Add
        // earlier" tile's label without a full recalculation; AddEarlierDay/Month/Season/Year
        // trigger a recalculation when the user actually adds one, refreshing the buffer.
        return new RecentObservationsOptions
        {
            ReferenceDate = selectedReferenceDate,
            ComparisonEndMode = selectedComparisonEndMode,
            CompletenessThreshold = completenessThreshold,
            PreviousDayCount = periodSelection.PreviousDayCount + 1,
            PreviousMonthCount = periodSelection.PreviousMonthCount + 1,
            PreviousSeasonCount = periodSelection.PreviousSeasonCount + 1,
            PreviousYearCount = periodSelection.PreviousYearCount + 1,
        };
    }

    private string FormatDateInput(DateOnly? date)
    {
        return date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private RecentObservationsTabState GetState(string domainKey)
    {
        if (!tabStates.TryGetValue(domainKey, out var state))
        {
            state = new RecentObservationsTabState();
            tabStates[domainKey] = state;
        }

        return state;
    }

    private bool IsBeforeMonthControls(RecentObservationTileViewModel tile)
    {
        return tile.PeriodKind is
            RecentObservationPeriodKind.Daily or
            RecentObservationPeriodKind.LatestSevenDays or
            RecentObservationPeriodKind.Month;
    }

    private bool IsSeasonTile(RecentObservationTileViewModel tile)
    {
        return tile.PeriodKind == RecentObservationPeriodKind.Season;
    }

    private bool IsAfterSeasonControls(RecentObservationTileViewModel tile)
    {
        return !IsBeforeMonthControls(tile) && !IsSeasonTile(tile);
    }

    private bool IsVisibleTile(RecentObservationTileViewModel tile)
    {
        return periodSelection.IsVisible(tile);
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

        // >= 1 excludes the offset-0 "to date" tile: "add earlier" only ever walks the complete
        // past periods, never the current one.
        return CurrentState.Result.Tiles
            .Where(tile => tile.PeriodKind == periodKind && tile.PeriodOffset is >= 1)
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
