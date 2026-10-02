namespace ClimateExplorer.Web.UiModel;

// Persisted by name in chart URLs. Old URLs use "MovingAverage", which ChartSeriesListSerializer
// maps to CentredMovingAverage. Append new values so existing ordinals don't change.
public enum SeriesSmoothingOptions
{
    None,
    CentredMovingAverage,
    Trendline,
    ShrinkingCentredMovingAverage,
}
