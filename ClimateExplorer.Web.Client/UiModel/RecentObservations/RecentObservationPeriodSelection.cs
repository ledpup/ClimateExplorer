namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

using System.Globalization;

public sealed class RecentObservationPeriodSelection
{
    public const int DefaultPreviousDayCount = 1;
    public const int DefaultPreviousMonthCount = 0;
    public const int DefaultPreviousSeasonCount = 0;

    private readonly SortedSet<int> visiblePreviousDayOffsets = new() { DefaultPreviousDayCount };
    private readonly SortedSet<int> visiblePreviousMonthOffsets = [];
    private readonly SortedSet<int> visiblePreviousSeasonOffsets = [];
    private readonly SortedSet<int> visiblePreviousYearOffsets = [];
    private readonly HashSet<RecentObservationPeriodKind> removedSingletonPeriodKinds = [];
    private bool defaultsSeeded;

    public int PreviousDayCount => visiblePreviousDayOffsets.Count;
    public int PreviousMonthCount => visiblePreviousMonthOffsets.Count;
    public int PreviousSeasonCount => visiblePreviousSeasonOffsets.Count;
    public int PreviousYearCount => visiblePreviousYearOffsets.Count;
    public bool IsAddEarlierDayDisabled => !CanAddEarlierDay();
    public bool IsAddEarlierMonthDisabled => !CanAddEarlierMonth();
    public bool IsAddEarlierSeasonDisabled => !CanAddEarlierSeason();
    public bool IsAddEarlierYearDisabled => !CanAddEarlierYear();

    /// <summary>
    /// Ensures a month/season/year tile is always visible by default: if a domain's tiles don't
    /// include the "current" to-date period (because it isn't meaningful yet - e.g. day 1 of the
    /// month, the first month of a season, or January), seeds the corresponding "previous" tile
    /// (offset 1) as visible instead. Only seeds once per reset cycle, so it won't fight a user
    /// who removes the seeded tile.
    /// </summary>
    public void EnsureDefaults(IEnumerable<RecentObservationTileViewModel> tiles)
    {
        if (defaultsSeeded)
        {
            return;
        }

        defaultsSeeded = true;

        var tileList = tiles as ICollection<RecentObservationTileViewModel> ?? tiles.ToList();
        SeedIfCurrentPeriodMissing(tileList, RecentObservationPeriodKind.CurrentMonth, visiblePreviousMonthOffsets);
        SeedIfCurrentPeriodMissing(tileList, RecentObservationPeriodKind.CurrentSeason, visiblePreviousSeasonOffsets);
        SeedIfCurrentPeriodMissing(tileList, RecentObservationPeriodKind.YearToDate, visiblePreviousYearOffsets);
    }

    public void AddEarlierDay(IEnumerable<int>? availableOffsets = null)
    {
        AddNextVisibleOffset(visiblePreviousDayOffsets, availableOffsets);
    }

    public void AddEarlierMonth(IEnumerable<int>? availableOffsets = null)
    {
        AddNextVisibleOffset(visiblePreviousMonthOffsets, availableOffsets);
    }

    public void AddEarlierSeason(IEnumerable<int>? availableOffsets = null)
    {
        AddNextVisibleOffset(visiblePreviousSeasonOffsets, availableOffsets);
    }

    public void AddEarlierYear(IEnumerable<int>? availableOffsets = null)
    {
        AddNextVisibleOffset(visiblePreviousYearOffsets, availableOffsets);
    }

    public bool CanAddEarlierDay(IEnumerable<int>? availableOffsets = null)
    {
        return GetNextVisibleOffset(visiblePreviousDayOffsets, availableOffsets).HasValue;
    }

    public bool CanAddEarlierMonth(IEnumerable<int>? availableOffsets = null)
    {
        return GetNextVisibleOffset(visiblePreviousMonthOffsets, availableOffsets).HasValue;
    }

    public bool CanAddEarlierSeason(IEnumerable<int>? availableOffsets = null)
    {
        return GetNextVisibleOffset(visiblePreviousSeasonOffsets, availableOffsets).HasValue;
    }

    public bool CanAddEarlierYear(IEnumerable<int>? availableOffsets = null)
    {
        return GetNextVisibleOffset(visiblePreviousYearOffsets, availableOffsets).HasValue;
    }

    public string CreateAddButtonLabel(
        RecentObservationPeriodKind periodKind,
        IEnumerable<RecentObservationTileViewModel> availableTiles,
        string fallbackPeriodName)
    {
        var tile = GetNextAddTile(periodKind, availableTiles);
        return tile is null
            ? $"Add {fallbackPeriodName}"
            : $"Add {CreateAddButtonPeriodLabel(tile)}";
    }

    public RecentObservationTileViewModel? GetNextAddTile(
        RecentObservationPeriodKind periodKind,
        IEnumerable<RecentObservationTileViewModel> availableTiles)
    {
        var tiles = GetAddableTiles(periodKind, availableTiles).ToList();
        var nextOffset = GetNextVisibleOffset(
            GetVisibleOffsets(periodKind),
            tiles.Select(tile => tile.PeriodOffset!.Value));

        return nextOffset.HasValue
            ? tiles.FirstOrDefault(tile => tile.PeriodOffset == nextOffset.Value)
            : null;
    }

    public bool IsVisible(RecentObservationTileViewModel tile)
    {
        if (!tile.PeriodOffset.HasValue)
        {
            return tile.PeriodKind is not RecentObservationPeriodKind.PreviousMonth
                and not RecentObservationPeriodKind.PreviousSeason
                and not RecentObservationPeriodKind.PreviousYear
                && !removedSingletonPeriodKinds.Contains(tile.PeriodKind);
        }

        return tile.PeriodKind switch
        {
            RecentObservationPeriodKind.Daily => visiblePreviousDayOffsets.Contains(tile.PeriodOffset.Value),
            RecentObservationPeriodKind.PreviousMonth => visiblePreviousMonthOffsets.Contains(tile.PeriodOffset.Value),
            RecentObservationPeriodKind.PreviousSeason => visiblePreviousSeasonOffsets.Contains(tile.PeriodOffset.Value),
            RecentObservationPeriodKind.PreviousYear => visiblePreviousYearOffsets.Contains(tile.PeriodOffset.Value),
            _ => true,
        };
    }

    public void Remove(RecentObservationTileViewModel tile)
    {
        if (!tile.PeriodOffset.HasValue)
        {
            removedSingletonPeriodKinds.Add(tile.PeriodKind);
            return;
        }

        switch (tile.PeriodKind)
        {
            case RecentObservationPeriodKind.Daily:
                visiblePreviousDayOffsets.Remove(tile.PeriodOffset.Value);
                break;
            case RecentObservationPeriodKind.PreviousMonth:
                visiblePreviousMonthOffsets.Remove(tile.PeriodOffset.Value);
                break;
            case RecentObservationPeriodKind.PreviousSeason:
                visiblePreviousSeasonOffsets.Remove(tile.PeriodOffset.Value);
                break;
            case RecentObservationPeriodKind.PreviousYear:
                visiblePreviousYearOffsets.Remove(tile.PeriodOffset.Value);
                break;
        }
    }

    public void Reset()
    {
        visiblePreviousDayOffsets.Clear();
        visiblePreviousDayOffsets.Add(DefaultPreviousDayCount);
        visiblePreviousMonthOffsets.Clear();
        visiblePreviousSeasonOffsets.Clear();
        visiblePreviousYearOffsets.Clear();
        removedSingletonPeriodKinds.Clear();
        defaultsSeeded = false;
    }

    private static void SeedIfCurrentPeriodMissing(
        ICollection<RecentObservationTileViewModel> tiles,
        RecentObservationPeriodKind currentPeriodKind,
        SortedSet<int> visibleOffsets)
    {
        if (!tiles.Any(tile => tile.PeriodKind == currentPeriodKind))
        {
            visibleOffsets.Add(1);
        }
    }

    private IEnumerable<RecentObservationTileViewModel> GetAddableTiles(
        RecentObservationPeriodKind periodKind,
        IEnumerable<RecentObservationTileViewModel> availableTiles)
    {
        return availableTiles
            .Where(tile => tile.PeriodKind == periodKind &&
                           tile.PeriodOffset.HasValue)
            .OrderBy(tile => tile.PeriodOffset!.Value);
    }

    private string CreateAddButtonPeriodLabel(RecentObservationTileViewModel tile)
    {
        return tile.PeriodKind switch
        {
            RecentObservationPeriodKind.PreviousMonth => tile.PeriodStartDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture),
            RecentObservationPeriodKind.PreviousYear => tile.PeriodStartDate.ToString("yyyy", CultureInfo.CurrentCulture),
            _ => tile.PeriodTitle,
        };
    }

    private SortedSet<int> GetVisibleOffsets(RecentObservationPeriodKind periodKind)
    {
        return periodKind switch
        {
            RecentObservationPeriodKind.Daily => visiblePreviousDayOffsets,
            RecentObservationPeriodKind.PreviousMonth => visiblePreviousMonthOffsets,
            RecentObservationPeriodKind.PreviousSeason => visiblePreviousSeasonOffsets,
            RecentObservationPeriodKind.PreviousYear => visiblePreviousYearOffsets,
            _ => [],
        };
    }

    private void AddNextVisibleOffset(SortedSet<int> visibleOffsets, IEnumerable<int>? availableOffsets)
    {
        var nextOffset = GetNextVisibleOffset(visibleOffsets, availableOffsets);
        if (nextOffset.HasValue)
        {
            visibleOffsets.Add(nextOffset.Value);
        }
    }

    private int? GetNextVisibleOffset(SortedSet<int> visibleOffsets, IEnumerable<int>? availableOffsets)
    {
        var currentMaxOffset = visibleOffsets.Count == 0 ? 0 : visibleOffsets.Max;
        var offsets = availableOffsets ?? [currentMaxOffset + 1];

        return offsets
            .Where(offset => offset > currentMaxOffset)
            .Order()
            .FirstOrDefault() is var nextOffset && nextOffset > 0
                ? nextOffset
                : null;
    }
}
