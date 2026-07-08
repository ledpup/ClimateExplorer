namespace ClimateExplorer.Web.Client.Components.Location;

using System.Globalization;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

public partial class RecentObservationTile
{
    private bool IsDayRecordsSelected => SelectedGroup?.Key == MetricGroupKey.DayRecords;

    private static string FormatCurrentMetricDate(DateOnly date)
    {
        return date.ToString("d MMM", CultureInfo.InvariantCulture);
    }

    private string FormatRecordOccurrence(RecentObservationMetricRecordViewModel record)
    {
        if (IsDayRecordsSelected && record.Date.HasValue)
        {
            return $" · {record.Date.Value.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";
        }

        return record.Year is not null ? $" ({record.Year})" : string.Empty;
    }
}
