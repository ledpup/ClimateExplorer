namespace ClimateExplorer.Web.Client.Services.Chart;

public interface IDefaultChartProvider
{
    ChartState CreateDefault(ChartPageContext context);
}
