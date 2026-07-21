namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed record TrendStatRow(
    string Label,
    string Value,
    bool IsEmphasized,
    string? AbstractExplanation,
    string? ClimateExplanation,
    IReadOnlyList<string>? WorkedExamples)
{
    public bool HasExplanation => AbstractExplanation is not null || ClimateExplanation is not null || WorkedExamples is not null;
}
