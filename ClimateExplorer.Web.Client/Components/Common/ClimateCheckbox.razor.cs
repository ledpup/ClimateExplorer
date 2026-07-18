namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

public partial class ClimateCheckbox
{
    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public bool Checked { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public EventCallback<bool> CheckedChanged { get; set; }

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
        await CheckedChanged.InvokeAsync((bool)e.Value!);
    }
}
