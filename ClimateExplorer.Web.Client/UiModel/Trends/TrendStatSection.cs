namespace ClimateExplorer.Web.Client.UiModel.Trends;

public sealed record TrendStatSection(string Title, IReadOnlyList<TrendStatRow> Rows);
