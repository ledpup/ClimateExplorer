namespace ClimateExplorer.Web.Client.Services.Chart;

public interface IRegionalAndGlobalDefaultChartProvider
{
    ChartState CreateDefault(RegionalAndGlobalDefaultChartContext context);
}
