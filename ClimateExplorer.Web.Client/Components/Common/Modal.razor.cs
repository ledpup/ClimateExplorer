namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>
/// A modal dialog with a green header (title and close button) and a scrolling body. It's a native
/// <c>&lt;dialog&gt;</c> opened with <c>showModal()</c>, so the browser keeps focus inside it, closes it on Escape
/// and returns focus to the opener; Modal.razor.js adds closing on a backdrop click. Open it with
/// <see cref="ShowAsync"/> or <c>@bind-Visible</c>. The body isn't rendered while the modal is closed, unless
/// <see cref="RenderContentWhenClosed"/> is set.
/// </summary>
public partial class Modal : IAsyncDisposable
{
    private readonly string titleId = $"climate-modal-{Guid.NewGuid():N}-title";
    private ElementReference dialog;
    private IJSObjectReference? module;
    private IJSObjectReference? controller;
    private TaskCompletionSource? rendered;
    private bool isOpen;
    private bool visibleParameter;
    private bool? pendingVisible;

    /// <summary>
    /// The title text. Use <see cref="TitleContent"/> instead for a title with markup.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public RenderFragment? TitleContent { get; set; }

    [Parameter]
    public ModalSize Size { get; set; }

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    /// <summary>
    /// Raised once the dialog is showing and its body has rendered.
    /// </summary>
    [Parameter]
    public EventCallback Opened { get; set; }

    /// <summary>
    /// Raised when the dialog closes, whether from the close button, Escape, a backdrop click or <see cref="HideAsync"/>.
    /// </summary>
    [Parameter]
    public EventCallback Closed { get; set; }

    /// <summary>
    /// Keeps the body rendered while the modal is closed. By default it's only rendered while open, so each opening
    /// starts with fresh content.
    /// </summary>
    [Parameter]
    public bool RenderContentWhenClosed { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    private string CssClass
    {
        get
        {
            var classes = Size switch
            {
                ModalSize.Large => "modal modal--large",
                ModalSize.ExtraLarge => "modal modal--extra-large",
                _ => "modal",
            };

            return string.IsNullOrWhiteSpace(Class)
                ? classes
                : $"{classes} {Class}";
        }
    }

    /// <summary>
    /// Opens the modal. Does nothing before the page is interactive, or if it's already open.
    /// </summary>
    public async Task ShowAsync()
    {
        if (isOpen || !RendererInfo.IsInteractive || JsRuntime is null)
        {
            return;
        }

        isOpen = true;

        // Render the body before showing the dialog, so the browser can move focus into it
        rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        StateHasChanged();
        await rendered.Task;

        var modal = await GetControllerAsync();
        await modal.InvokeVoidAsync("show");

        if (!Visible)
        {
            await VisibleChanged.InvokeAsync(true);
        }

        await Opened.InvokeAsync();
    }

    /// <summary>
    /// Closes the modal. <see cref="Closed"/> is raised when the dialog's close event arrives.
    /// </summary>
    public async Task HideAsync()
    {
        try
        {
            if (controller is not null)
            {
                await controller.InvokeVoidAsync("close");
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (controller is not null)
            {
                await controller.InvokeVoidAsync("dispose");
                await controller.DisposeAsync();
            }

            if (module is not null)
            {
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }

    protected override void OnParametersSet()
    {
        // Act on a change of Visible only, so a parent that doesn't bind it can't reopen a modal the user closed
        if (Visible != visibleParameter)
        {
            visibleParameter = Visible;
            pendingVisible = Visible == isOpen ? null : Visible;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        rendered?.TrySetResult();
        rendered = null;

        if (pendingVisible is bool visible)
        {
            pendingVisible = null;
            await (visible ? ShowAsync() : HideAsync());
        }
    }

    private async Task<IJSObjectReference> GetControllerAsync()
    {
        module ??= await JsRuntime!.InvokeAsync<IJSObjectReference>("import", "./Components/Common/Modal.razor.js");
        controller ??= await module.InvokeAsync<IJSObjectReference>("init", dialog);
        return controller;
    }

    private async Task OnDialogClosedAsync()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;

        if (Visible)
        {
            await VisibleChanged.InvokeAsync(false);
        }

        await Closed.InvokeAsync();
    }
}
