namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Web.Client.UiModel;

public sealed record ChartLocationSubstitutionResult(
    ChartState State,
    IReadOnlyList<UserNotification> Messages);
