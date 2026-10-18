namespace ClimateExplorer.Web.Client.UiModel.Trends;

/// <summary>
/// What the location dashboard's trend <c>OverviewField</c> shows: its label, value and (multiline) tooltip.
/// </summary>
public sealed record LocationTrendField(string Label, string Value, string Tooltip);
