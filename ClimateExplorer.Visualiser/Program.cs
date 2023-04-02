using ClimateExplorer.Visualiser;
using ClimateExplorer.Visualiser.Services;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorCurrentDevice;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddBlazorise(options =>
    {
    })
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons()
    .AddMapService()
    .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    .AddSingleton<IDataServiceCache, DataServiceCache>()
    .AddTransient<IExporter, Exporter>()
    .AddBlazorCurrentDevice()
    .AddBlazoredLocalStorage()
    .AddHttpClient<IDataService, DataService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["dataServiceBaseUri"]);
    }); 

await builder.Build().RunAsync();
