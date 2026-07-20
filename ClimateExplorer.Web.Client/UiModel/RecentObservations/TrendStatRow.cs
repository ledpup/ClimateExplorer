namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed record TrendStatRow(
    string Label,
    string Value,
    bool IsEmphasized,
    string? AbstractExplanation,
    string? ClimateExplanation,
    string? WorkedExample)
{
    public bool HasExplanation => AbstractExplanation is not null || ClimateExplanation is not null || WorkedExample is not null;
}
