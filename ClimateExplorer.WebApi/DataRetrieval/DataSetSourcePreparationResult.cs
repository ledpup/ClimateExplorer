namespace ClimateExplorer.WebApi.DataRetrieval;

using System;

internal sealed record DataSetSourcePreparationResult(
    DataSetSourcePreparationOutcome Outcome,
    DateTimeOffset? RetrievedDate = null);
