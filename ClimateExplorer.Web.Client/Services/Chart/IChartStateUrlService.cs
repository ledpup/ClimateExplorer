namespace ClimateExplorer.Web.Client.Services.Chart;

public interface IChartStateUrlService
{
    ChartUrlStateResult Parse(Uri uri, ChartPageContext context);

    string BuildRelativeUrl(string pagePath, ChartState state);
}
