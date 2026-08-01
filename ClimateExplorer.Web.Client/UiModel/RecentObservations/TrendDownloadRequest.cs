namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

using ClimateExplorer.Core.Stats.Model;

public sealed record TrendDownloadRequest
{
    public string DataTypeLabel { get; init; } = string.Empty;
    public string WindowLabel { get; init; } = string.Empty;
    public IReadOnlyList<DataPoint> Points { get; init; } = [];
}
