#pragma warning disable SA1200 // Using directives should be placed correctly
using BlazorCurrentDevice;
using Blazored.LocalStorage;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using ClimateExplorer.Web.Services;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#pragma warning restore SA1200 // Using directives should be placed correctly

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services
    .AddBlazorise()
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons()
    .AddMapService()
    .AddScoped(sp => new HttpClient())
    .AddSingleton<IDataServiceCache, DataServiceCache>()
    .AddTransient<IExporter, Exporter>()
    .AddCurrentDeviceService()
    .AddBlazoredLocalStorage()
    .AddHttpClient<IDataService, DataService>(client =>
    {
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
        client.BaseAddress = new Uri(builder.Configuration["DataServiceBaseUri"]!);
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
    });

await builder.Build().RunAsync();
