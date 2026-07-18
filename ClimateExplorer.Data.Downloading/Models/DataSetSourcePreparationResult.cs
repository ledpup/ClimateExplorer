namespace ClimateExplorer.Data.Downloading.Models;

public sealed record DataSetSourcePreparationResult(
    DataSetSourcePreparationOutcome Outcome,
    DateTimeOffset? RetrievedDate = null);
