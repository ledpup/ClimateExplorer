namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

public partial class SidePanel : ComponentBase
{
    private bool isVisible;
    private bool isAnimatingIn;
    private bool isAnimatingOut;

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string Width { get; set; } = "80%";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public async Task ShowAsync()
    {
        isVisible = true;
        isAnimatingIn = true;
        StateHasChanged();

        await Task.Delay(100);
        isAnimatingIn = false;
        StateHasChanged();
    }

    private async Task DismissAsync()
    {
        isAnimatingOut = true;
        StateHasChanged();

        await Task.Delay(400);

        isVisible = false;
        isAnimatingOut = false;
        StateHasChanged();
    }
}
