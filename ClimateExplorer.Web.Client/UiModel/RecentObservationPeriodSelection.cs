namespace ClimateExplorer.Web.Client.UiModel;

public sealed class RecentObservationPeriodSelection
{
    public const int DefaultPreviousMonthCount = 0;
    public const int DefaultPreviousSeasonCount = 0;
    public const int MaximumPreviousMonthCount = 11;
    public const int MaximumPreviousSeasonCount = 3;

    public int PreviousMonthCount { get; private set; } = DefaultPreviousMonthCount;
    public int PreviousSeasonCount { get; private set; } = DefaultPreviousSeasonCount;
    public bool IsAddEarlierMonthDisabled => PreviousMonthCount >= MaximumPreviousMonthCount;
    public bool IsRemoveMonthDisabled => PreviousMonthCount == 0;
    public bool IsAddEarlierSeasonDisabled => PreviousSeasonCount >= MaximumPreviousSeasonCount;
    public bool IsRemoveSeasonDisabled => PreviousSeasonCount == 0;

    public void AddEarlierMonth()
    {
        SetPreviousMonthCount(PreviousMonthCount + 1);
    }

    public void RemoveMonth()
    {
        SetPreviousMonthCount(PreviousMonthCount - 1);
    }

    public void AddEarlierSeason()
    {
        SetPreviousSeasonCount(PreviousSeasonCount + 1);
    }

    public void RemoveSeason()
    {
        SetPreviousSeasonCount(PreviousSeasonCount - 1);
    }

    public void Reset()
    {
        PreviousMonthCount = DefaultPreviousMonthCount;
        PreviousSeasonCount = DefaultPreviousSeasonCount;
    }

    private void SetPreviousMonthCount(int count)
    {
        PreviousMonthCount = Math.Clamp(count, 0, MaximumPreviousMonthCount);
    }

    private void SetPreviousSeasonCount(int count)
    {
        PreviousSeasonCount = Math.Clamp(count, 0, MaximumPreviousSeasonCount);
    }
}
