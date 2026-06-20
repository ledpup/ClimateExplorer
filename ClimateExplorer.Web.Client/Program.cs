#pragma warning disable SA1200 // Using directives should be placed correctly
using Blazored.LocalStorage;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using ClimateExplorer.Core.Blog;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.Services.Chart;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.WebApiClient.Services;
using CurrentDevice;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#pragma warning restore SA1200 // Using directives should be placed correctly

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services
    .AddBlazorise(options =>
    {
        options.ProductToken = "CjxRBnF9NAo9UwNzfDA1BlEAc3g0Cj5QAHF+NQg8bjoNJ2ZdYhBVCCo/Cj9bCUxERldhE1EvN0xcNm46FD1gSkUHCkxESVFvBl4yK1FBfAYKEytiTWACQkxEWmdIImQACVdxSDxvDA9dZ1MxfxYdWmc2LX8eAkx1RTdjTERaZ002ZA4NSnVcL3UVC1pnQSJoHhFXd1swbx50S3dTL3kMB1FrAWlvHg1NeV43Yx4RSHlUPG8TAVJrUzwKDwFadEUueRUdCDJTPHwIHVFuRSZnHhFIeVQ8bxMBUmtTPAoPAVp0RS55FR0IMlM8ZBMLQG5FJmceEUh5VDxvEwFSa1M8Cg8BWnRFLnkVHQgyRzNbbitMemBbZDUXalUnMHwzP0JJeVUDCnhoTyciAAkkdAtNKGBuG38NbQ1YD3dnTFsyWyADaHJ+NFMjNDRXTgZ8BT02E0s5XiUcM1ojKmAMCFFbeFN0cWFTaWEmeDkpYmJmSH00P39bIyplKz5tE00ncy0WKldKM1QXPE0NYQ5/CRxXWVYqdxt3S1lDBkFqNkYPR0xXDWFLAFZbfzgrN0x7B395B3RpNDYAfA==";
    })
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons()
    .AddMapService()
    .AddScoped(sp => new HttpClient())
    .AddSingleton<IDataServiceCache, DataServiceCache>()
    .AddTransient<IExporter, Exporter>()
    .AddScoped<IInfoPanelDismissalService, InfoPanelDismissalService>()
    .AddScoped<IChartStateUrlService, ChartStateUrlService>()
    .AddScoped<ILocationDefaultChartProvider, LocationDefaultChartProvider>()
    .AddScoped<IRegionalAndGlobalDefaultChartProvider, RegionalAndGlobalDefaultChartProvider>()
    .AddScoped<IChartSeriesLocationSubstitutionService, ChartSeriesLocationSubstitutionService>()
    .AddScoped<IChartDataBuilder, ChartDataBuilder>()
    .AddScoped<IRecentObservationsDataProvider, RecentObservationsDataProvider>()
    .AddScoped<IRecentObservationsCalculator, RecentObservationsCalculator>()
    .AddScoped<IRecentObservationsService, RecentObservationsService>()
    .AddScoped<ISiteOverviewService, SiteOverviewService>()
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

await builder.Build().RunAsync();
