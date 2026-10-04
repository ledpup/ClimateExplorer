namespace ClimateExplorer.Web;

using ClimateExplorer.Web.UiLogic;

public static class DateLabelsFactory
{
    /// <summary>
    /// Builds the reader's <see cref="DateLabels"/> from the Accept-Language header of the request
    /// being served, so dates are in their order from the very first (prerendered) response.
    /// </summary>
    public static DateLabels Create(IServiceProvider services)
    {
        var request = services.GetService<IHttpContextAccessor>()?.HttpContext?.Request;

        return new DateLabels
        {
            MonthFirst = DateLabels.IsMonthFirstAcceptLanguage(request?.Headers.AcceptLanguage.ToString()),
        };
    }
}
