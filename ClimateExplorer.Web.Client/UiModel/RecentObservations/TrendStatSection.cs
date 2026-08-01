namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed record TrendStatSection(string Title, IReadOnlyList<TrendStatRow> Rows);
