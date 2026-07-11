namespace ClimateExplorer.Web.Client.Layout;

using ClimateExplorer.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

public partial class NavMenu : IDisposable
{
    private readonly (string Route, string Text)[] primaryItems =
                [
                    (string.Empty, "local"),
                    ("global", "global"),
                ];

    private readonly SecondaryNavItem[] secondaryItems =
                [
                    SecondaryNavItem.Link("locations", "locations"),
                    SecondaryNavItem.Link("blog", "blog"),
                    SecondaryNavItem.Link("about", "about"),
                    SecondaryNavItem.SiteOverview("site overview"),
                ];

    [Inject]
    public ISiteOverviewService? SiteOverviewService { get; set; }

    [Inject]
    public NavigationManager? NavigationManager { get; set; }

    private IEnumerable<SecondaryNavItem> VisibleSecondaryItems => secondaryItems.Where(item => !item.LocalOnly || IsLocalPage());

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

    public bool IsHamburgerCurrentPage() => VisibleSecondaryItems.Any(item => item.Route is not null && IsCurrentPage(item.Route));

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

    private bool IsLocalPage()
    {
        var path = NavigationManager!.ToAbsoluteUri(NavigationManager.Uri).LocalPath;

        return path == "/" || IsLocal(path);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        StateHasChanged();
    }

    private sealed record SecondaryNavItem(string? Route, string Text, bool LocalOnly, bool OpensSiteOverview)
    {
        public static SecondaryNavItem Link(string route, string text)
        {
            return new(route, text, LocalOnly: false, OpensSiteOverview: false);
        }

        public static SecondaryNavItem SiteOverview(string text)
        {
            return new(Route: null, text, LocalOnly: true, OpensSiteOverview: true);
        }
    }
}
