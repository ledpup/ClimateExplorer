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
                ];

    private readonly (string Route, string Text)[] hamburgerItems =
                [
                    ("locations", "locations"),
                    ("blog", "blog"),
                    ("about", "about"),
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
            // The "local" page is the root page, but also includes any URL that starts with "/location" (but not "/locations").
            // Not the best route naming!
            return path == "/" || IsLocal(path);
        }

        return path == $"/{route}" || path.StartsWith($"/{route}/");
    }

    public bool IsHamburgerCurrentPage() => hamburgerItems.Any(item => IsCurrentPage(item.Route));

    public void Dispose()
    {
        NavigationManager!.LocationChanged -= OnLocationChanged;
    }

    protected override void OnInitialized()
    {
        NavigationManager!.LocationChanged += OnLocationChanged;
    }

    private static bool IsLocal(string path)
    {
        return path.StartsWith("/location") && !path.StartsWith("/locations");
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        StateHasChanged();
    }
}
