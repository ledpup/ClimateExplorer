using AcornSat.Visualiser;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Darnton.Blazor.DeviceInterop.Geolocation;
using FisSst.BlazorMaps.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddBlazorise(options =>
    {
        options.ChangeTextOnKeyPress = true;
    })
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons()
    .AddBlazorLeafletMaps()
    .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    .AddScoped<IGeolocationService, GeolocationService>()
    .AddHttpClient<IDataService, DataService>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:54836/");
    }); 

await builder.Build().RunAsync();
