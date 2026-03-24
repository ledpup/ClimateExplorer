namespace ClimateExplorer.Web.Client.Layout;

using ClimateExplorer.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

public partial class NavMenu : IDisposable
{
    [Inject]
    public ISiteOverviewService? SiteOverviewService { get; set; }

    [Inject]
    public NavigationManager? NavigationManager { get; set; }

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
