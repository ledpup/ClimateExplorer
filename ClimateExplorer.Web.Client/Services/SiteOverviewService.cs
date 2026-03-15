namespace ClimateExplorer.Web.Client.Services;

public class SiteOverviewService : ISiteOverviewService
{
    public event Action? ShowRequested;

    public void RequestShow() => ShowRequested?.Invoke();
}
