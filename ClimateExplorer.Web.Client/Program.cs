#pragma warning disable SA1200 // Using directives should be placed correctly
using Blazored.LocalStorage;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using ClimateExplorer.Web.Services;
using ClimateExplorer.WebApiClient.Services;
using CurrentDevice;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#pragma warning restore SA1200 // Using directives should be placed correctly

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services
    .AddBlazorise(options =>
    {
        options.ProductToken = "CjxRBnF9NAk+UAB4ejM1BlEAc3s0Cj5QAHl6Mww/bjoNJ2ZdYhBVCCo/Cj9bCUxERldhE1EvN0xcNm46FD1gSkUHCkxESVFvBl4yK1FBfAYKEytiTWACQkxEWmdIImQACVdxSDxvDA9dZ1MxfxYdWmc2LX8eAkx1RTdjTERaZ002ZA4NSnVcL3UVC1pnQSJoHhFXd1swbx50S3dTL3kMB1FrAWlvHg1NeV43Yx4RSHlUPG8TAVJrUzwKDwFadEUueRUdCDJTPHwIHVFuRSZnHhFIeVQ8bxMBUmtTPAoPAVp0RS55FR0IMlM8ZBMLQG5FJmceEUh5VDxvEwFSa1M8Cg8BWnRFLnkVHQgyajV6DS1CcEVbH3kWc2F6VlcSACp6SFN0OH9he0o5ei4qRG9BU2AZNj1iXi4CJik1WVUFSSkabFFrBFUidk1ZRhVgLjxNE2Q5ARAIf0xlVQMzCT1Xe1sbOStOUlkMRQo6TEx2MQEsLWR9SBtTIwFPYjsHVAsDSU05LQcCOGhqSwtXNyp2SFYrAyJ6MFZ+CHE3AkNiPRdUdXhtaGtQAjs0NlFHLVsRJU4JYVcAfA==";
    })
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons()
    .AddMapService()
    .AddScoped(sp => new HttpClient())
    .AddSingleton<IDataServiceCache, DataServiceCache>()
    .AddTransient<IExporter, Exporter>()
    .AddScoped<IInfoPanelDismissalService, InfoPanelDismissalService>()
    .AddScoped<ISiteOverviewService, SiteOverviewService>()
    .AddCurrentDeviceService()
    .AddBlazoredLocalStorage()
    .AddHttpClient<IDataService, DataService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["DataServiceBaseUri"] !);
    });

await builder.Build().RunAsync();