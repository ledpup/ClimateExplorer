namespace ClimateExplorer.Web.Client.Components.Common;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>
/// A tab strip of <see cref="Tab"/> children (ARIA tabs pattern, automatic activation). It only renders the tabs;
/// the parent renders the selected content. A parent that wants the panel associated with its tab renders
/// <c>role="tabpanel" aria-labelledby="@Tabs.IdFor(tabsId, name)"</c> and passes the same <see cref="Id"/>.
/// </summary>
public partial class Tabs : IAsyncDisposable
{
    private readonly List<Tab> tabs = [];
    private readonly string generatedId = $"climate-tabs-{Guid.NewGuid():N}";
    private ElementReference tablist;
    private IJSObjectReference? module;
    private string? renderedSelectedTab;

    [Parameter]
    public string? SelectedTab { get; set; }

    [Parameter]
    public EventCallback<string> SelectedTabChanged { get; set; }

    [Parameter]
    [EditorRequired]
    public string AriaLabel { get; set; } = string.Empty;

    /// <summary>
    /// The prefix of the tab ids. Only needed when the parent refers to a tab by id; see <see cref="IdFor"/>.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject]
    private IJSRuntime? JsRuntime { get; set; }

    private string CssClass =>
        string.IsNullOrWhiteSpace(Class)
            ? "climate-tabs"
            : $"climate-tabs {Class}";

    /// <summary>
    /// The id of the tab called <paramref name="name"/> in the strip whose <see cref="Id"/> is
    /// <paramref name="tabsId"/>.
    /// </summary>
    public static string IdFor(string tabsId, string name) => $"{tabsId}-{NonIdCharacters().Replace(name, "-")}";

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (module is not null)
            {
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }

    internal string TabIdFor(string name) => IdFor(Id ?? generatedId, name);

    /// <summary>
    /// Whether the tab is the one in the Tab order: the selected tab or, when no tab is selected, the first enabled
    /// one, so the strip can still be reached from the keyboard.
    /// </summary>
    internal bool IsTabStop(Tab tab) =>
        tab.Name == SelectedTab
        || (!tabs.Any(x => x.Name == SelectedTab) && tabs.FirstOrDefault(x => !x.Disabled) == tab);

    internal void Register(Tab tab)
    {
        tabs.Add(tab);
        RefreshTabsIfNoneSelected();
    }

    internal void Unregister(Tab tab)
    {
        tabs.Remove(tab);
        RefreshTabsIfNoneSelected();
    }

    internal async Task SelectAsync(string name)
    {
        if (name != SelectedTab)
        {
            await SelectedTabChanged.InvokeAsync(name);
        }
    }

    internal async Task OnKeyDownAsync(Tab source, string key)
    {
        if (!TabNavigation.IsNavigationKey(key) || JsRuntime is null)
        {
            return;
        }

        module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Common/Tabs.razor.js");
        var ids = await module.InvokeAsync<string[]>("getTabIds", tablist);
        var ordered = ids
            .Select(id => tabs.FirstOrDefault(x => x.TabId == id))
            .OfType<Tab>()
            .ToList();

        var current = ordered.IndexOf(source);
        if (current < 0)
        {
            return;
        }

        var next = TabNavigation.NextIndex(current, ordered.Count, i => ordered[i].Disabled, key);
        if (next == current)
        {
            return;
        }

        await ordered[next].FocusAsync();
        await SelectAsync(ordered[next].Name);
    }

    protected override void OnParametersSet()
    {
        // The tabs only re-render when their own parameters change, so tell them when the selection moves
        if (renderedSelectedTab != SelectedTab)
        {
            renderedSelectedTab = SelectedTab;
            RefreshTabs();
        }
    }

    [GeneratedRegex("[^A-Za-z0-9_-]")]
    private static partial Regex NonIdCharacters();

    private void RefreshTabsIfNoneSelected()
    {
        // Adding or removing a tab can move the fallback tab stop
        if (!tabs.Any(x => x.Name == SelectedTab))
        {
            RefreshTabs();
        }
    }

    private void RefreshTabs()
    {
        foreach (var tab in tabs)
        {
            tab.Refresh();
        }
    }
}
