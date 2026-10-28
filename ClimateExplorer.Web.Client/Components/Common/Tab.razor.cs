namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>
/// A tab of the enclosing <see cref="Tabs"/>. <see cref="ChildContent"/> is the label.
/// </summary>
public partial class Tab : IDisposable
{
    private ElementReference element;

    [CascadingParameter]
    public Tabs? Parent { get; set; }

    [Parameter]
    [EditorRequired]
    public string Name { get; set; } = string.Empty;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    internal string? TabId => Parent?.TabIdFor(Name);

    private bool IsSelected => Parent?.SelectedTab == Name;

    private bool IsTabStop => Parent?.IsTabStop(this) ?? false;

    private string CssClass => IsSelected ? "climate-tab is-active" : "climate-tab";

    public void Dispose()
    {
        Parent?.Unregister(this);
    }

    internal void Refresh() => StateHasChanged();

    internal ValueTask FocusAsync() => element.FocusAsync();

    protected override void OnInitialized()
    {
        Parent?.Register(this);
    }

    private Task SelectAsync() => Parent?.SelectAsync(Name) ?? Task.CompletedTask;

    private Task OnKeyDownAsync(KeyboardEventArgs e) => Parent?.OnKeyDownAsync(this, e.Key) ?? Task.CompletedTask;
}
