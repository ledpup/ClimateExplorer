namespace ClimateExplorer.Web.Client.Services.InfoPanelDismissal;

public interface IInfoPanelDismissalService
{
    Task<bool> ShouldShowAsync(string panelName, string version);
    Task DismissAsync(string panelName, string version);
}
