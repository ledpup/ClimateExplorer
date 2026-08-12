namespace ClimateExplorer.Web.Client.UiModel.Trends;

/// <summary>
/// What a trend is being described *about* - the label and unit used throughout the statistical
/// breakdown. Keeping this separate from any one view model is what lets the Recent Observations
/// tile and a chart series share the trend statistics builder rather than growing a second
/// statistics vocabulary.
/// </summary>
public sealed record TrendStatSubject(string Label, string Unit);
