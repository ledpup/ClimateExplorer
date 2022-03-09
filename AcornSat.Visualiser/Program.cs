using AcornSat.Visualiser;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using FisSst.BlazorMaps.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddBlazorise(options =>
    {
    })
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons()
    .AddBlazorLeafletMaps()
    .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    .AddHttpClient<IDataService, DataService>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:54836/");
    });

await builder.Build().RunAsync();
