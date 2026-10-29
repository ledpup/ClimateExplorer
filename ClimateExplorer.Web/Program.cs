#pragma warning disable SA1200 // Using directives should be placed correctly
using Blazored.LocalStorage;
using ClimateExplorer.Core.Blog;
using ClimateExplorer.Web;
using ClimateExplorer.Web.Client.Pages;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.Client.Services.InfoPanelDismissal;
using ClimateExplorer.Web.Client.Services.Notifications;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.WebApiClient.Services;
using CurrentDevice;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#pragma warning restore SA1200 // Using directives should be placed correctly

var builder = WebApplication.CreateBuilder(args);

// Console output isn't captured on Windows App Service; this feeds App Service logs (filesystem/log stream)
// instead, and does nothing when not running there.
builder.Logging.AddAzureWebAppDiagnostics();

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

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
    .AddScoped(DateLabelsFactory.Create)
    .AddScoped<UnitPreference>()
    .AddCurrentDeviceService()
    .AddBlazoredLocalStorage()
    .AddHttpClient<IDataService, DataService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["DataServiceBaseUri"]!);
    });

builder.Services.AddSingleton<IBlogService, ClimateExplorer.Web.Blog.BlogService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapStaticAssets();

app.MapGet("/api/blog/posts", async (IBlogService blog) => await blog.GetAllPostsAsync());

app.MapGet("/blog/rss.xml", async (IBlogService blog) =>
{
    var posts = await blog.GetAllPostsAsync();
    var xml = ClimateExplorer.Web.Blog.RssFeedGenerator.Build(posts);
    return Results.Bytes(xml, "application/rss+xml; charset=utf-8");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(About).Assembly);

await app.RunAsync();
