namespace ClimateExplorer.Web.Client.Services.Chart;

public interface IChartStateUrlService
{
    ChartUrlStateResult Parse(Uri uri, ChartUrlStateContext context);

    string BuildRelativeUrl(string pagePath, ChartState state);
}
