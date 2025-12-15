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
        options.ProductToken = "CjxRBXB8NA08VAFwfDM1BlEAc3s0Cj5QAHl6Mww/bjoNJ2ZdYhBVCCo/Cj9bCUxERldhE1EvN0xcNm46FD1gSkUHCkxESVFvBl4yK1FBfAYKEytiTWACQkxEWmdIImQACVdxSDxvDA9dZ1MxfxYdWmc2LX8eAkx1RTdjTERaZ002ZA4NSnVcL3UVC1pnQSJoHhFXd1swbx50S3dTL3kMB1FrAWlvHg1NeV43Yx4RSHlUPG8TAVJrUzwKDwFadEUueRUdCDJTPHwIHVFuRSZnHhFIeVQ8bxMBUmtTPAoPAVp0RS55FR0IMlM8ZBMLQG5FJmceEUh5VDxvEwFSa1M8Cg8BWnRFLnkVHQgyXiZ9K3dOVHsvZjIXQUlrNkVyGmtgWFVTOyBJUmQOeRIqNmFOVWUxeFB3VA1Udh5TaDhWWiV4RktEF3QlCGp7OBFWKSRQdGUhWXEjcnlfNGQ3IG5fPCdDeSxUb0hQQQ4ZTU1/AHl5GFdMblR6ICoycmYvBSYrXw1vGXQxCjJ5ehFmDhZzaUQ5SigbbghDLEcJHU1PdFcBCiNKXDoSVDQ2UHtIDwMHJ2YISSFffA==";
    })
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
        client.BaseAddress = new Uri(builder.Configuration["DataServiceBaseUri"] !);
    });

await builder.Build().RunAsync();