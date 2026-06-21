namespace ClimateExplorer.Web.Client.Services.Chart;

public interface IChartSeriesLocationSubstitutionService
{
    ChartLocationSubstitutionResult Substitute(ChartLocationSubstitutionContext context);
}
