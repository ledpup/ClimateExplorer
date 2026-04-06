namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

public partial class DelayedLoadingIndicator : IDisposable
{
    private bool showIndicator = false;

    private bool delaying = false;

    private CancellationTokenSource cts = new();

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public int ZIndex { get; set; }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Visible)
        {
            if (!delaying)
            {
                delaying = true;
                _ = DelayIndicatorAsync();
            }
        }
        else
        {
            showIndicator = false;
        }
    }

    private async Task DelayIndicatorAsync()
    {
        try
        {
            // One second delay before showing the indicator to avoid flickering for fast operations
            await Task.Delay(1000, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        delaying = false;
        var currentIndicatorState = showIndicator;
        showIndicator = Visible;
        if (currentIndicatorState != showIndicator)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
}
