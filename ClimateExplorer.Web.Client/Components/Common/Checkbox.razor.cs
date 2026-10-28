namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

public partial class Checkbox
{
    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public bool Value { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// The id of the <c>&lt;input&gt;</c>, so a <c>&lt;label for&gt;</c> elsewhere can name it.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public EventCallback<bool> ValueChanged { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass =>
        string.IsNullOrWhiteSpace(Class)
            ? "climate-checkbox"
            : $"climate-checkbox {Class}";

    private async Task OnChanged(ChangeEventArgs e)
    {
        await ValueChanged.InvokeAsync((bool)e.Value!);
    }
}
