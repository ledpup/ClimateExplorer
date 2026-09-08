namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

using System.Globalization;

public sealed class RecentObservationPeriodSelection
{
    public const int DefaultPreviousDayCount = 1;
    public const int DefaultPreviousMonthCount = 0;
    public const int DefaultPreviousSeasonCount = 0;

    private readonly SortedSet<int> visiblePreviousDayOffsets = new() { DefaultPreviousDayCount };
    private readonly SortedSet<int> visibleMonthOffsets = [0];
    private readonly SortedSet<int> visibleSeasonOffsets = [0];
    private readonly SortedSet<int> visibleYearOffsets = [0];
    private bool isLatestSevenDaysRemoved;
    private bool defaultsSeeded;

    public int PreviousDayCount => visiblePreviousDayOffsets.Count;
    public int PreviousMonthCount => visibleMonthOffsets.Count(offset => offset > 0);
    public int PreviousSeasonCount => visibleSeasonOffsets.Count(offset => offset > 0);
    public int PreviousYearCount => visibleYearOffsets.Count(offset => offset > 0);
    public bool IsAddEarlierDayDisabled => !CanAddEarlierDay();
    public bool IsAddEarlierMonthDisabled => !CanAddEarlierMonth();
    public bool IsAddEarlierSeasonDisabled => !CanAddEarlierSeason();
    public bool IsAddEarlierYearDisabled => !CanAddEarlierYear();

    /// <summary>
    /// Ensures a month/season/year tile is always visible by default: if a domain's tiles don't
    /// include the "current" to-date period (offset 0 - because it isn't meaningful yet, e.g. day
    /// 1 of the month, the first month of a season, or January), seeds the corresponding offset-1
    /// tile as visible instead. Only seeds once per reset cycle, so it won't fight a user who
    /// removes the seeded tile.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a tile was seeded, growing one of the visible counts. Calculations
    /// only request a small lookahead buffer beyond what's currently visible (see
    /// <c>RecentObservationsPanel.CreateOptions</c>), so callers should recalculate when this
    /// returns <see langword="true"/> to refresh that buffer for the newly-visible tile.
    /// </returns>
    public bool EnsureDefaults(IEnumerable<RecentObservationTileViewModel> tiles)
    {
        if (defaultsSeeded)
        {
            return false;
        }

        defaultsSeeded = true;

        var tileList = tiles as ICollection<RecentObservationTileViewModel> ?? tiles.ToList();
        var seededMonth = SeedIfCurrentPeriodMissing(tileList, RecentObservationPeriodKind.Month, visibleMonthOffsets);
        var seededSeason = SeedIfCurrentPeriodMissing(tileList, RecentObservationPeriodKind.Season, visibleSeasonOffsets);
        var seededYear = SeedIfCurrentPeriodMissing(tileList, RecentObservationPeriodKind.Year, visibleYearOffsets);
        return seededMonth || seededSeason || seededYear;
    }

    public void AddEarlierDay(IEnumerable<int>? availableOffsets = null)
    {
        AddNextVisibleOffset(visiblePreviousDayOffsets, availableOffsets);
    }

    public void AddEarlierMonth(IEnumerable<int>? availableOffsets = null)
    {
        AddNextVisibleOffset(visibleMonthOffsets, availableOffsets);
    }

    public void AddEarlierSeason(IEnumerable<int>? availableOffsets = null)
    {
        AddNextVisibleOffset(visibleSeasonOffsets, availableOffsets);
    }

    public void AddEarlierYear(IEnumerable<int>? availableOffsets = null)
    {
        AddNextVisibleOffset(visibleYearOffsets, availableOffsets);
    }

    public bool CanAddEarlierDay(IEnumerable<int>? availableOffsets = null)
    {
        return GetNextVisibleOffset(visiblePreviousDayOffsets, availableOffsets).HasValue;
    }

    public bool CanAddEarlierMonth(IEnumerable<int>? availableOffsets = null)
    {
        return GetNextVisibleOffset(visibleMonthOffsets, availableOffsets).HasValue;
    }

    public bool CanAddEarlierSeason(IEnumerable<int>? availableOffsets = null)
    {
        return GetNextVisibleOffset(visibleSeasonOffsets, availableOffsets).HasValue;
    }

    public bool CanAddEarlierYear(IEnumerable<int>? availableOffsets = null)
    {
        return GetNextVisibleOffset(visibleYearOffsets, availableOffsets).HasValue;
    }

    public string CreateAddButtonLabel(
        RecentObservationPeriodKind periodKind,
        IEnumerable<RecentObservationTileViewModel> availableTiles,
        string fallbackPeriodName)
    {
        var tile = GetNextAddTile(periodKind, availableTiles);
        return tile is null
            ? fallbackPeriodName
            : CreateAddButtonPeriodLabel(tile);
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
        if (tile.PeriodKind == RecentObservationPeriodKind.LatestSevenDays)
        {
            return !isLatestSevenDaysRemoved;
        }

        return tile.PeriodOffset.HasValue && GetVisibleOffsets(tile.PeriodKind).Contains(tile.PeriodOffset.Value);
    }

    /// <summary>
    /// Makes a family's "to date" tile (offset 0) visible again if it had previously been hidden
    /// by <see cref="Remove"/> - used by a bulk "add N earlier" action that needs the current
    /// month/season to anchor the set it's about to fill in, regardless of whether it was already
    /// showing (e.g. after the caller's own clear-and-refill, or an earlier manual remove-tile
    /// click).
    /// </summary>
    public void EnsureVisible(RecentObservationPeriodKind kind)
    {
        GetVisibleOffsets(kind).Add(0);
    }

    public void Remove(RecentObservationTileViewModel tile)
    {
        if (tile.PeriodKind == RecentObservationPeriodKind.LatestSevenDays)
        {
            isLatestSevenDaysRemoved = true;
            return;
        }

        if (tile.PeriodOffset.HasValue)
        {
            GetVisibleOffsets(tile.PeriodKind).Remove(tile.PeriodOffset.Value);
        }
    }

    public void Reset()
    {
        visiblePreviousDayOffsets.Clear();
        visiblePreviousDayOffsets.Add(DefaultPreviousDayCount);
        visibleMonthOffsets.Clear();
        visibleMonthOffsets.Add(0);
        visibleSeasonOffsets.Clear();
        visibleSeasonOffsets.Add(0);
        visibleYearOffsets.Clear();
        visibleYearOffsets.Add(0);
        isLatestSevenDaysRemoved = false;
        defaultsSeeded = false;
    }

    private static bool SeedIfCurrentPeriodMissing(
        ICollection<RecentObservationTileViewModel> tiles,
        RecentObservationPeriodKind kind,
        SortedSet<int> visibleOffsets)
    {
        if (tiles.Any(tile => tile.PeriodKind == kind && tile.PeriodOffset == 0))
        {
            return false;
        }

        visibleOffsets.Add(1);
        return true;
    }

    private IEnumerable<RecentObservationTileViewModel> GetAddableTiles(
        RecentObservationPeriodKind periodKind,
        IEnumerable<RecentObservationTileViewModel> availableTiles)
    {
        // >= 1 excludes the offset-0 "to date" tile: it's never something you "add earlier" to
        // get more of, it's the fixed anchor the earlier-period series counts back from.
        return availableTiles
            .Where(tile => tile.PeriodKind == periodKind && tile.PeriodOffset is >= 1)
            .OrderBy(tile => tile.PeriodOffset!.Value);
    }

    private string CreateAddButtonPeriodLabel(RecentObservationTileViewModel tile)
    {
        return tile.PeriodKind switch
        {
            RecentObservationPeriodKind.Month => tile.PeriodStartDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture),
            RecentObservationPeriodKind.Year => tile.PeriodStartDate.ToString("yyyy", CultureInfo.CurrentCulture),
            _ => tile.PeriodTitle,
        };
    }

    private SortedSet<int> GetVisibleOffsets(RecentObservationPeriodKind periodKind)
    {
        return periodKind switch
        {
            RecentObservationPeriodKind.Daily => visiblePreviousDayOffsets,
            RecentObservationPeriodKind.Month => visibleMonthOffsets,
            RecentObservationPeriodKind.Season => visibleSeasonOffsets,
            RecentObservationPeriodKind.Year => visibleYearOffsets,
            _ => throw new ArgumentOutOfRangeException(nameof(periodKind), periodKind, "LatestSevenDays has no offset series."),
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
