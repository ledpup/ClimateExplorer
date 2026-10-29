namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

/// <summary>
/// A text box that raises <see cref="ValueChanged"/> on <c>change</c> (blur or Enter), not on each keystroke.
/// It doesn't validate: the parent decides, and passes <see cref="Invalid"/> and <see cref="ErrorMessage"/> back.
/// </summary>
public partial class TextInput
{
    private readonly string errorId = $"text-input-error-{Guid.NewGuid():N}";

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// The id of the <c>&lt;input&gt;</c>, so a <c>&lt;label for&gt;</c> elsewhere can name it.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>Goes on the wrapper, which holds the input and the error message.</summary>
    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Shown under the input, and used as its description, while <see cref="Invalid"/> is true.</summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private bool ShowError => Invalid && !string.IsNullOrEmpty(ErrorMessage);

    private string WrapCssClass =>
        string.IsNullOrWhiteSpace(Class)
            ? "text-input-wrap"
            : $"text-input-wrap {Class}";

    private string CssClass =>
        Invalid
            ? "control text-input text-input--invalid"
            : "control text-input";

    private async Task OnChangeAsync(ChangeEventArgs e)
    {
        await ValueChanged.InvokeAsync(e.Value as string);
    }
}
