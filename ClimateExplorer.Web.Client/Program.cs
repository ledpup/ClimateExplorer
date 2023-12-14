using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using DPBlazorMapLibrary;
using ClimateExplorer.Web.Services;
using BlazorCurrentDevice;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services
    .AddBlazorise()
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons()
    .AddMapService()
    .AddScoped(sp => new HttpClient())
    .AddSingleton<IDataServiceCache, DataServiceCache>()
    .AddTransient<IExporter, Exporter>()
    .AddBlazorCurrentDevice()
    .AddBlazoredLocalStorage()
    .AddHttpClient<IDataService, DataService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["DataServiceBaseUri"]!);
    });

await builder.Build().RunAsync();
