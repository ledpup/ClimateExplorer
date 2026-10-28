namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>
/// A text box that suggests matching items as you type (ARIA 1.2 editable combobox with list autocomplete and
/// manual selection). Matching is a case-insensitive "contains" on <see cref="TextField"/>, prefix matches first
/// (see <see cref="AutocompleteFilter"/>). The text doesn't have to match an item. Choosing an option, by click or
/// with the arrow keys and Enter, raises <see cref="SelectedValueChanged"/>.
/// </summary>
public partial class Autocomplete<TItem, TValue> : IAsyncDisposable
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(120);

    private readonly string listId = $"climate-autocomplete-{Guid.NewGuid():N}";
    private ElementReference input;
    private ElementReference list;
    private IJSObjectReference? module;
    private IJSObjectReference? controller;
    private CancellationTokenSource? debounce;
    private IReadOnlyList<TItem> results = [];
    private bool capped;
    private bool open;
    private int activeIndex = -1;
    private bool scrollToActive;
    private string? text;
    private string? selectedTextParameter;
    private string? statusText;

    [Parameter]
    public IEnumerable<TItem>? Data { get; set; }

    [Parameter]
    [EditorRequired]
    public Func<TItem, string> TextField { get; set; } = _ => string.Empty;

    [Parameter]
    [EditorRequired]
    public Func<TItem, TValue> ValueField { get; set; } = _ => default!;

    /// <summary>How many characters (after trimming) must be typed before any options show.</summary>
    [Parameter]
    public int MinSearchLength { get; set; } = 1;

    /// <summary>The most options shown at once. When more match, a footer asks for more typing.</summary>
    [Parameter]
    public int MaxResults { get; set; } = 50;

    [Parameter]
    public string MaxMenuHeight { get; set; } = "350px";

    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Names the input. Needed unless a <c>&lt;label for&gt;</c> points at <see cref="Id"/>.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// The id of the <c>&lt;input&gt;</c>, so a <c>&lt;label for&gt;</c> elsewhere can name it.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>Goes on the wrapper, which holds the input and the list.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>The text in the input. Use <c>@bind-SelectedText</c>.</summary>
    [Parameter]
    public string? SelectedText { get; set; }

    [Parameter]
    public EventCallback<string?> SelectedTextChanged { get; set; }

    /// <summary>Raised with the chosen option's <see cref="ValueField"/>.</summary>
    [Parameter]
    public EventCallback<TValue> SelectedValueChanged { get; set; }

    /// <summary>
    /// Renders <c>autofocus</c>. A <see cref="Modal"/> moves focus to it when it opens.
    /// </summary>
    [Parameter]
    public bool AutoFocus { get; set; }

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    private bool IsOpen => open && results.Count > 0;

    private string? ActiveOptionId => IsOpen && activeIndex >= 0 ? OptionId(activeIndex) : null;

    private string WrapCssClass => string.IsNullOrWhiteSpace(Class)
        ? "climate-autocomplete"
        : $"climate-autocomplete {Class}";

    public ValueTask FocusAsync() => input.FocusAsync();

    public async ValueTask DisposeAsync()
    {
        CancelDebounce();

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
        // Act on a change from the parent only: when the binding echoes back what was typed, the list stays as it is
        if (SelectedText != selectedTextParameter)
        {
            selectedTextParameter = SelectedText;

            if (SelectedText != text)
            {
                text = SelectedText;
                CancelDebounce();
                ClearResults();
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RendererInfo.IsInteractive && JsRuntime is not null)
        {
            try
            {
                module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Common/Autocomplete.razor.js");
                controller = await module.InvokeAsync<IJSObjectReference>("init", input, list);
            }
            catch (JSDisconnectedException)
            {
            }
        }

        if (scrollToActive && controller is not null)
        {
            scrollToActive = false;
            await controller.InvokeVoidAsync("scrollActiveIntoView");
        }
    }

    private string OptionId(int index) => $"{listId}-option-{index}";

    private async Task OnInputAsync(ChangeEventArgs e)
    {
        text = e.Value as string;

        // The options shown are for the old text until the filter runs, so Enter mustn't pick one
        activeIndex = -1;
        await SelectedTextChanged.InvokeAsync(text);

        // Filter once typing pauses, rather than re-rendering the list for every keystroke
        CancelDebounce();
        debounce = new CancellationTokenSource();
        var token = debounce.Token;

        try
        {
            await Task.Delay(DebounceDelay, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        // Cancelled after the delay finished but before this continuation ran (by a blur, say)
        if (token.IsCancellationRequested)
        {
            return;
        }

        UpdateResults();
        open = true;
    }

    // Autocomplete.razor.js cancels the browser's default for these keys, so Escape with the list open doesn't also
    // close a dialog the input is in
    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowDown":
                MoveActive(1);
                break;
            case "ArrowUp":
                MoveActive(-1);
                break;
            case "Enter":
                if (IsOpen && activeIndex >= 0)
                {
                    await ChooseAsync(activeIndex);
                }

                break;
            case "Escape":
                if (IsOpen)
                {
                    Close();
                }
                else if (!string.IsNullOrEmpty(text))
                {
                    text = string.Empty;
                    CancelDebounce();
                    ClearResults();
                    await SelectedTextChanged.InvokeAsync(text);
                }

                break;
        }
    }

    private void OnBlur()
    {
        // A pending filter would otherwise open the list after focus has gone
        CancelDebounce();
        Close();
    }

    private void MoveActive(int direction)
    {
        if (!IsOpen)
        {
            // The list may be closed, or not yet filtered for the latest text
            CancelDebounce();
            UpdateResults();
            open = true;

            if (results.Count > 0)
            {
                activeIndex = direction > 0 ? 0 : results.Count - 1;
                scrollToActive = true;
            }

            return;
        }

        activeIndex = activeIndex < 0
            ? (direction > 0 ? 0 : results.Count - 1)
            : (activeIndex + direction + results.Count) % results.Count;
        scrollToActive = true;
    }

    private async Task ChooseAsync(int index)
    {
        var item = results[index];

        CancelDebounce();
        Close();

        text = TextField(item);
        await SelectedTextChanged.InvokeAsync(text);
        await SelectedValueChanged.InvokeAsync(ValueField(item));
    }

    private void UpdateResults()
    {
        results = AutocompleteFilter.Filter(Data ?? [], TextField, text, MinSearchLength, MaxResults, out capped);
        activeIndex = -1;

        var queryLength = text?.Trim().Length ?? 0;
        statusText = queryLength == 0 || queryLength < MinSearchLength
            ? null
            : results.Count switch
            {
                0 => "No results",
                1 => "1 result",
                _ when capped => $"More than {MaxResults} results. Keep typing to narrow them.",
                _ => $"{results.Count} results",
            };
    }

    private void ClearResults()
    {
        results = [];
        capped = false;
        statusText = null;
        Close();
    }

    private void Close()
    {
        open = false;
        activeIndex = -1;
    }

    private void CancelDebounce()
    {
        debounce?.Cancel();
        debounce?.Dispose();
        debounce = null;
    }
}
