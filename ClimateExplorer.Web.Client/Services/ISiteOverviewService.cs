namespace ClimateExplorer.Web.Client.Services;

public interface ISiteOverviewService
{
    event Action? ShowRequested;
    void RequestShow();
}
