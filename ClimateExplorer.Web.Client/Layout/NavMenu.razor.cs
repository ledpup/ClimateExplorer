namespace ClimateExplorer.Web.Client.Layout;

using ClimateExplorer.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

public partial class NavMenu : IDisposable
{
    private readonly (string Route, string Text)[] navItems =
                [
                    (string.Empty, "local"),
                        ("regionalandglobal", "regional & global"),
                        ("about", "about"),
                        ("blog", "blog"),
                    ];

    [Inject]
    public ISiteOverviewService? SiteOverviewService { get; set; }

    [Inject]
    public NavigationManager? NavigationManager { get; set; }

    public bool IsCurrentPage(string route)
    {
        var path = NavigationManager!.ToAbsoluteUri(NavigationManager.Uri).LocalPath;

        if (route == string.Empty)
        {
            return path == "/" || path.StartsWith("/location");
        }

        return path == $"/{route}" || path.StartsWith($"/{route}/");
    }

    public void Dispose()
    {
        NavigationManager!.LocationChanged -= OnLocationChanged;
    }

    protected override void OnInitialized()
    {
        NavigationManager!.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        StateHasChanged();
    }
}
