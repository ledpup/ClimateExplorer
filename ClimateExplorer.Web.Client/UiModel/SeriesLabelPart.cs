namespace ClimateExplorer.Web.UiModel;

/// <summary>
/// What a <see cref="SeriesLabelSegment"/> describes, so labels can be compared part by part
/// before they're joined into text.
/// </summary>
public enum SeriesLabelPart
{
    Location,
    Year,
    DataType,
    Adjustment,
    Transformation,
    Aggregation,
    TemporalCalculation,
    Value,
    Smoothing,

    /// <summary>An override or derived-series label, kept whole.</summary>
    Whole,
}
