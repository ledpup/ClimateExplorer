namespace ClimateExplorer.Data.Downloading.Models;

/// <param name="Outcome">What the caller should do with its cached response.</param>
/// <param name="RetrievedDate">The oldest retrieval time of the current sources, when known.</param>
/// <param name="LatestRecordDate">
/// The oldest of the current sources' latest daily record dates (see <see cref="Core.Model.DataSetSourceState.LatestRecordDate"/>),
/// when every source records one. Lets a caller check that a cached result built from these sources actually
/// reflects their current content, not just that it was saved recently.
/// </param>
public sealed record DataSetSourcePreparationResult(
    DataSetSourcePreparationOutcome Outcome,
    DateTimeOffset? RetrievedDate = null,
    DateOnly? LatestRecordDate = null);
