namespace ClimateExplorer.Web.Client.UiModel;

using System.Globalization;

public sealed class RecentObservationPeriodSelection
{
    public const int DefaultPreviousDayCount = 1;
    public const int DefaultPreviousMonthCount = 0;
    public const int DefaultPreviousSeasonCount = 0;
    public const int MaximumPreviousDayCount = 7;
    public const int MaximumPreviousMonthCount = 11;
    public const int MaximumPreviousSeasonCount = 3;

    private readonly SortedSet<int> visiblePreviousDayOffsets = new() { DefaultPreviousDayCount };
    private readonly SortedSet<int> visiblePreviousMonthOffsets = [];
    private readonly SortedSet<int> visiblePreviousSeasonOffsets = [];

    public int PreviousDayCount => visiblePreviousDayOffsets.Count;
    public int PreviousMonthCount => visiblePreviousMonthOffsets.Count;
    public int PreviousSeasonCount => visiblePreviousSeasonOffsets.Count;
    public bool IsAddEarlierDayDisabled => !CanAddEarlierDay();
    public bool IsAddEarlierMonthDisabled => !CanAddEarlierMonth();
    public bool IsAddEarlierSeasonDisabled => !CanAddEarlierSeason();

    public void AddEarlierDay(IEnumerable<int>? availableOffsets = null)
    {
        AddNextVisibleOffset(visiblePreviousDayOffsets, availableOffsets, MaximumPreviousDayCount);
    }

    public void AddEarlierMonth(IEnumerable<int>? availableOffsets = null)
    {
        AddNextVisibleOffset(visiblePreviousMonthOffsets, availableOffsets, MaximumPreviousMonthCount);
    }

    public void AddEarlierSeason(IEnumerable<int>? availableOffsets = null)
    {
        AddNextVisibleOffset(visiblePreviousSeasonOffsets, availableOffsets, MaximumPreviousSeasonCount);
    }

    public bool CanAddEarlierDay(IEnumerable<int>? availableOffsets = null)
    {
        return GetNextVisibleOffset(visiblePreviousDayOffsets, availableOffsets, MaximumPreviousDayCount).HasValue;
    }

    public bool CanAddEarlierMonth(IEnumerable<int>? availableOffsets = null)
    {
        return GetNextVisibleOffset(visiblePreviousMonthOffsets, availableOffsets, MaximumPreviousMonthCount).HasValue;
    }

    public bool CanAddEarlierSeason(IEnumerable<int>? availableOffsets = null)
    {
        return GetNextVisibleOffset(visiblePreviousSeasonOffsets, availableOffsets, MaximumPreviousSeasonCount).HasValue;
    }

    public string CreateAddButtonLabel(
        RecentObservationPeriodKind periodKind,
        IEnumerable<RecentObservationTileViewModel> availableTiles,
        string fallbackPeriodName)
    {
        var tile = GetNextAddTile(periodKind, availableTiles) ?? GetMaximumReachedTile(periodKind, availableTiles);
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
            tiles.Select(tile => tile.PeriodOffset!.Value),
            GetMaximumOffset(periodKind));

        return nextOffset.HasValue
            ? tiles.FirstOrDefault(tile => tile.PeriodOffset == nextOffset.Value)
            : null;
    }

    public bool IsVisible(RecentObservationTileViewModel tile)
    {
        if (!tile.PeriodOffset.HasValue)
        {
            return tile.PeriodKind is not RecentObservationPeriodKind.PreviousMonth and not RecentObservationPeriodKind.PreviousSeason;
        }

        return tile.PeriodKind switch
        {
            RecentObservationPeriodKind.Daily => visiblePreviousDayOffsets.Contains(tile.PeriodOffset.Value),
            RecentObservationPeriodKind.PreviousMonth => visiblePreviousMonthOffsets.Contains(tile.PeriodOffset.Value),
            RecentObservationPeriodKind.PreviousSeason => visiblePreviousSeasonOffsets.Contains(tile.PeriodOffset.Value),
            _ => true,
        };
    }

    public bool IsRemovable(RecentObservationTileViewModel tile)
    {
        if (!tile.PeriodOffset.HasValue || !IsVisible(tile))
        {
            return false;
        }

        return tile.PeriodKind switch
        {
            RecentObservationPeriodKind.Daily => tile.PeriodOffset.Value > DefaultPreviousDayCount,
            RecentObservationPeriodKind.PreviousMonth => true,
            RecentObservationPeriodKind.PreviousSeason => true,
            _ => false,
        };
    }

    public void Remove(RecentObservationTileViewModel tile)
    {
        if (!tile.PeriodOffset.HasValue || !IsRemovable(tile))
        {
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
        }
    }

    public void Reset()
    {
        visiblePreviousDayOffsets.Clear();
        visiblePreviousDayOffsets.Add(DefaultPreviousDayCount);
        visiblePreviousMonthOffsets.Clear();
        visiblePreviousSeasonOffsets.Clear();
    }

    private RecentObservationTileViewModel? GetMaximumReachedTile(
        RecentObservationPeriodKind periodKind,
        IEnumerable<RecentObservationTileViewModel> availableTiles)
    {
        var visibleOffsets = GetVisibleOffsets(periodKind);
        if (visibleOffsets.Count == 0 || visibleOffsets.Max < GetMaximumOffset(periodKind))
        {
            return null;
        }

        return GetAddableTiles(periodKind, availableTiles)
            .Where(tile => visibleOffsets.Contains(tile.PeriodOffset!.Value))
            .OrderByDescending(tile => tile.PeriodOffset!.Value)
            .FirstOrDefault();
    }

    private IEnumerable<RecentObservationTileViewModel> GetAddableTiles(
        RecentObservationPeriodKind periodKind,
        IEnumerable<RecentObservationTileViewModel> availableTiles)
    {
        var maximumOffset = GetMaximumOffset(periodKind);

        return availableTiles
            .Where(tile => tile.PeriodKind == periodKind &&
                           tile.PeriodOffset.HasValue &&
                           tile.PeriodOffset.Value <= maximumOffset)
            .OrderBy(tile => tile.PeriodOffset!.Value);
    }

    private string CreateAddButtonPeriodLabel(RecentObservationTileViewModel tile)
    {
        return tile.PeriodKind == RecentObservationPeriodKind.PreviousMonth
            ? tile.PeriodStartDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture)
            : tile.PeriodTitle;
    }

    private SortedSet<int> GetVisibleOffsets(RecentObservationPeriodKind periodKind)
    {
        return periodKind switch
        {
            RecentObservationPeriodKind.Daily => visiblePreviousDayOffsets,
            RecentObservationPeriodKind.PreviousMonth => visiblePreviousMonthOffsets,
            RecentObservationPeriodKind.PreviousSeason => visiblePreviousSeasonOffsets,
            _ => [],
        };
    }

    private int GetMaximumOffset(RecentObservationPeriodKind periodKind)
    {
        return periodKind switch
        {
            RecentObservationPeriodKind.Daily => MaximumPreviousDayCount,
            RecentObservationPeriodKind.PreviousMonth => MaximumPreviousMonthCount,
            RecentObservationPeriodKind.PreviousSeason => MaximumPreviousSeasonCount,
            _ => 0,
        };
    }

    private void AddNextVisibleOffset(SortedSet<int> visibleOffsets, IEnumerable<int>? availableOffsets, int maximumOffset)
    {
        var nextOffset = GetNextVisibleOffset(visibleOffsets, availableOffsets, maximumOffset);
        if (nextOffset.HasValue)
        {
            visibleOffsets.Add(nextOffset.Value);
        }
    }

    private int? GetNextVisibleOffset(SortedSet<int> visibleOffsets, IEnumerable<int>? availableOffsets, int maximumOffset)
    {
        var currentMaxOffset = visibleOffsets.Count == 0 ? 0 : visibleOffsets.Max;
        var offsets = availableOffsets ?? Enumerable.Range(1, maximumOffset);

        return offsets
            .Where(offset => offset > currentMaxOffset && offset <= maximumOffset)
            .Order()
            .FirstOrDefault() is var nextOffset && nextOffset > 0
                ? nextOffset
                : null;
    }
}
