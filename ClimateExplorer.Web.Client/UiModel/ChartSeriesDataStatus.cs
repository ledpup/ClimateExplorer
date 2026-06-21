namespace ClimateExplorer.Web.UiModel;

public enum ChartSeriesDataStatus
{
    Rendered,
    NoRawData,
    NoChartableDataAfterCompletenessFiltering,
    NoChartableDataAfterSmoothing,
    FallbackToUnsmoothedData,
}
