namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using System.Globalization;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

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
        return date.ToString("d MMM", CultureInfo.InvariantCulture);
    }

    public static string FormatDayRecordOccurrence(RecentObservationMetricRecordViewModel record)
    {
        return record.Date.HasValue
            ? $" · {record.Date.Value.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}"
            : FormatPeriodRecordOccurrence(record);
    }

    public static string FormatPeriodRecordOccurrence(RecentObservationMetricRecordViewModel record)
    {
        return record.Year is not null ? $" ({record.Year})" : string.Empty;
    }
}
