namespace ClimateExplorer.Web.Client.Shared;

using ClimateExplorer.Web.Services;
using Microsoft.AspNetCore.Components;

public partial class InfoPanel : ComponentBase
{
    private bool isVisible;
    private bool isAnimatingIn;
    private bool isAnimatingOut;

    [Parameter]
    public string PanelName { get; set; } = string.Empty;

    [Parameter]
    public string Version { get; set; } = "1.0";

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string? Height { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject]
    private IInfoPanelDismissalService DismissalService { get; set; } = default!;

    public async Task ShowAsync()
    {
        isVisible = true;
        isAnimatingIn = true;
        StateHasChanged();

        await Task.Delay(50);
        isAnimatingIn = false;
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !string.IsNullOrEmpty(PanelName))
        {
            var shouldShow = await DismissalService.ShouldShowAsync(PanelName, Version);

            if (shouldShow)
            {
                await ShowAsync();
            }
        }
    }

    private async Task DismissAsync()
    {
        isAnimatingOut = true;
        StateHasChanged();

        await Task.Delay(400);

        isVisible = false;
        isAnimatingOut = false;

        if (!string.IsNullOrEmpty(PanelName))
        {
            await DismissalService.DismissAsync(PanelName, Version);
        }

        StateHasChanged();
    }
}
