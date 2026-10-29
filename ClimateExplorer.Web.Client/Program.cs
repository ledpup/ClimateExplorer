#pragma warning disable SA1200 // Using directives should be placed correctly
using Blazored.LocalStorage;
using ClimateExplorer.Core.Blog;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.Client.Services.InfoPanelDismissal;
using ClimateExplorer.Web.Client.Services.Notifications;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.WebApiClient.Services;
using CurrentDevice;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
#pragma warning restore SA1200 // Using directives should be placed correctly

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services
    .AddMapService()
    .AddScoped(sp => new HttpClient())
    .AddMemoryCache(options => options.SizeLimit = DataService.CacheSizeLimit)
    .AddTransient<IExporter, Exporter>()
    .AddScoped<IInfoPanelDismissalService, InfoPanelDismissalService>()
    .AddScoped<IChartStateUrlService, ChartStateUrlService>()
    .AddScoped<IChartSeriesLocationSubstitutionService, ChartSeriesLocationSubstitutionService>()
    .AddScoped<IChartDataBuilder, ChartDataBuilder>()
    .AddScoped<IUserNotificationService, NotificationService>()
    .AddScoped<IRecentObservationsDataProvider, RecentObservationsDataProvider>()
    .AddScoped<IRecentObservationsCalculator, RecentObservationsCalculator>()
    .AddScoped<IRecentObservationsService, RecentObservationsService>()
    .AddScoped<ISiteOverviewService, SiteOverviewService>()
    .AddScoped<DateLabels>()
    .AddScoped<UnitPreference>()
    .AddCurrentDeviceService()
    .AddBlazoredLocalStorage();

builder.Services.AddHttpClient<IBlogService, ClientBlogService>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

builder.Services.AddHttpClient<IDataService, DataService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["DataServiceBaseUri"]!);
});

var host = builder.Build();

// Fallback for when the server's choice of date order doesn't arrive with the prerendered page
try
{
    var browserLanguage = await host.Services.GetRequiredService<IJSRuntime>().GetValueAsync<string>("navigator.language");

    host.Services.GetRequiredService<DateLabels>().MonthFirst = DateLabels.IsMonthFirstLocale(browserLanguage);
}
catch (Exception)
{
    // Date order is a nicety - never let it stop the app from starting
}

await host.RunAsync();
