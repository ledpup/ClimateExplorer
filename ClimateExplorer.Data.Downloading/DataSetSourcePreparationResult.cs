namespace ClimateExplorer.Data.Downloading;

public sealed record DataSetSourcePreparationResult(
    DataSetSourcePreparationOutcome Outcome,
    DateTimeOffset? RetrievedDate = null);
