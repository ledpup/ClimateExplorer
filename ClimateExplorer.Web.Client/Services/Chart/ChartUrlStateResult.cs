namespace ClimateExplorer.Web.Client.Services.Chart;

public enum ChartUrlStateKind
{
    Missing,
    Valid,
    ExplicitEmpty,
    Invalid,
}

public sealed record ChartUrlStateResult(
    ChartUrlStateKind Kind,
    ChartState? State,
    string? ErrorMessage);
