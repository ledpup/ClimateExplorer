namespace ClimateExplorer.Web.Client.UiModel;

using ClimateExplorer.Core.Model;

/// <summary>
/// Reduces a tab's combined source metadata (e.g. TempMax + TempMin) down to one
/// entry per distinct download, so a single file backing multiple metrics (such as
/// a GHCNd station CSV) is only listed once even if its label differs per metric.
/// </summary>
public static class RecentObservationRetrievalMetadataSelector
{
    public static IReadOnlyList<RecentObservationSourceMetadata> Select(IEnumerable<RecentObservationSourceMetadata> sourceMetadata)
    {
        return sourceMetadata
            .Where(x => !string.IsNullOrWhiteSpace(x.SourceUrl) && x.RetrievedAtUtc.HasValue)
            .GroupBy(x => x.SourceUrl!, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.OrderByDescending(metadata => metadata.RetrievedAtUtc).First())
            .OrderBy(x => x.SourceUrlLabel ?? x.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
