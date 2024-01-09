using ClimateExplorer.Web;
using ClimateExplorer.Web.Services;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorCurrentDevice;
using Blazored.LocalStorage;
using ClimateExplorer.Web.Client.Pages;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

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

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(About).Assembly);

await app.RunAsync();
