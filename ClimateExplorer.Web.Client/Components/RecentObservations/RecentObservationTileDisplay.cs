namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using ClimateExplorer.Web.UiLogic;

internal static class RecentObservationTileDisplay
{
    public static string StatusClass(RecentObservationRecordStatus status) => status switch
    {
        RecentObservationRecordStatus.NewRecord => "new",
        RecentObservationRecordStatus.EqualRecord => "equal",
        _ => "none",
    };

    public static string FormatCurrentMetricDate(DateOnly date)
    {
        return DateLabels.ShortDayMonth(date);
    }

    public static string FormatDayRecordOccurrence(RecentObservationMetricRecordViewModel record)
    {
        return record.Date.HasValue
            ? $" · {DateLabels.ShortDayMonthYear(record.Date.Value)}"
            : FormatPeriodRecordOccurrence(record);
    }

    public static string FormatPeriodRecordOccurrence(RecentObservationMetricRecordViewModel record)
    {
        return record.Year is not null ? $" ({record.Year})" : string.Empty;
    }
}
