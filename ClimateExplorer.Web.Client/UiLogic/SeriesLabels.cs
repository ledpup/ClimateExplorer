namespace ClimateExplorer.Web.UiLogic;

/// <summary>
/// The output of <see cref="SeriesLabeller"/>, indexed like the series it was given.
/// </summary>
/// <param name="Labels">Each series' legend label.</param>
/// <param name="TrendPrefixes">The text that starts the legend label of each series' trend overlays.</param>
/// <param name="SeriesNote">The label parts every series shares, drawn once beside the legend. Null when there are none.</param>
public sealed record SeriesLabels(IReadOnlyList<string> Labels, IReadOnlyList<string> TrendPrefixes, string? SeriesNote);
