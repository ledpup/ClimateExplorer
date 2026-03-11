namespace ClimateExplorer.Web.Services;

public interface ISiteOverviewService
{
    event Action? ShowRequested;
    void RequestShow();
}

public class SiteOverviewService : ISiteOverviewService
{
    public event Action? ShowRequested;

    public void RequestShow() => ShowRequested?.Invoke();
}
