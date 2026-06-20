namespace ClimateExplorer.Web.Client.Services.Chart;

public interface ILocationDefaultChartProvider
{
    ChartState CreateDefault(LocationDefaultChartContext context);
}
