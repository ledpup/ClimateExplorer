namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

// Month/Season/Year each cover both the "to date" period and the complete past ones - see
// PeriodOffset: 0 means "to date" (the current, possibly partial, period), 1+ means "N periods
// ago" (a complete past period). Daily's PeriodOffset is unrelated (1-based "days ago", no 0
// case). LatestSevenDays has no offset at all - it's a single always-shown trailing window with
// no "previous" series.
public enum RecentObservationPeriodKind
{
    Daily,
    LatestSevenDays,
    Month,
    Season,
    Year,
}
