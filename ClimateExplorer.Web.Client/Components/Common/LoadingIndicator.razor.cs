namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

public partial class LoadingIndicator : IDisposable
{
    private const string DefaultText = "Loading…";

    private bool showIndicator = false;

    private bool delaying = false;

    private bool disposed;

    private CancellationTokenSource cts = new();

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public LoadingIndicatorVariant Variant { get; set; } = LoadingIndicatorVariant.Overlay;

    /// <summary>
    /// Visible text next to the spinner. When null, "Loading…" is still rendered for screen readers.
    /// </summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>
    /// How long <see cref="Visible"/> must stay true before the indicator shows, to avoid flicker on fast loads.
    /// </summary>
    [Parameter]
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Overlay only.
    /// </summary>
    [Parameter]
    public int? ZIndex { get; set; }

    /// <summary>
    /// Overlay: the content being covered. Inline: rendered in place of the indicator when not visible.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? Class { get; set; }

    private string EffectiveText => Text ?? DefaultText;

    private string? TextClass => Text is null ? "visually-hidden" : "loading-text";

    private string? ZIndexStyle => ZIndex is null ? null : $"z-index: {ZIndex}";

    public void Dispose()
    {
        disposed = true;
        cts.Cancel();
        cts.Dispose();
    }

    protected override void OnParametersSet()
    {
        if (Visible)
        {
            if (Delay <= TimeSpan.Zero)
            {
                showIndicator = true;
            }
            else if (!delaying && !showIndicator)
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
            await Task.Delay(Delay, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        delaying = false;

        if (disposed)
        {
            return;
        }

        var currentIndicatorState = showIndicator;
        showIndicator = Visible;
        if (currentIndicatorState != showIndicator)
        {
            try
            {
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception)
            {
                // The render tree may already be torn down by this point (e.g. Interactive
                // Auto's Server-to-WebAssembly handoff), even though our own Dispose() may not
                // have run yet. There's nothing left to update in that case.
            }
        }
    }
}
