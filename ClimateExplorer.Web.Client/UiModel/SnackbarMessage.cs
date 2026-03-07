namespace ClimateExplorer.Web.Client.UiModel;

using Blazorise.Snackbar;
public class SnackbarMessage
{
    public string Message { get; set; } = string.Empty;
    public SnackbarColor Type { get; set; } = SnackbarColor.Info;
}
