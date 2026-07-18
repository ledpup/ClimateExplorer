#pragma warning disable SA1200 // Using directives should be placed correctly
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json.Serialization;
using ClimateExplorer.Data.Downloading.Downloaders;
using ClimateExplorer.Data.Downloading.Orchestration;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.Data.Downloading.Transformers;
using ClimateExplorer.Data.Downloading.Workspace;
using ClimateExplorer.Data.Ghcnd;
using ClimateExplorer.WebApi;
using ClimateExplorer.WebApi.AcornSat;
using ClimateExplorer.WebApi.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#pragma warning restore SA1200 // Using directives should be placed correctly

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(
    options =>
    {
        options.AddDefaultPolicy(
            builder =>
            {
                // Don't limit which origins browsers will allow to invoke these web services. Why?
                //    1. the way GitHub actions deploys staging builds of Azure Static Web Apps generates
                //       a different client site DNS name each time. While the sequence is predictable, it'd
                //       be a pain to pre-register many names ahead of time, a pain to react to the allocated
                //       name at deployment time, and the wildcarding functionality (ref CorsPolicyBuilder.
                //       SetIsOriginAllowedToAllowWildcardSubdomains()) is limited to permitting all subdomains
                //       of a nominated domain, which would be overly permissive in our case. (Because the
                //       generated client site DNS name is of the form:
                //           lively-sky-06d813c1e-36.westus2.1.azurestaticapps.net
                //       It's the "36" which changes each time a deployment happens to staging.
                //
                //    2. Our users aren't authenticated. External web apps can't induce our users' browsers to
                //       do anything using their credentials against our API site because they don't have
                //       credentials, and can't modify any data via the API. The exposure is minimal.
                builder.AllowAnyOrigin();

                builder.AllowAnyHeader();
            });
    });

builder.Services.Configure<JsonOptions>(
    options =>
    {
        // This causes the JSON returned from API calls to omit properties if their value is null anyway
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Logging.AddConsole();

var everydayCache = new FileBackedTwoLayerCache("cache");
var longtermCache = new FileBackedTwoLayerCache("cache-longterm");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<DataSetFreshnessPolicy>();
builder.Services.AddSingleton<DataSetAssetLockProvider>();
builder.Services.AddSingleton<DataSetDownloadWorkspaceFactory>();
builder.Services.AddSingleton<DataSetDownloadValidator>();
builder.Services.AddSingleton(new DataSetSourceAssetResolver());
builder.Services.AddSingleton(new DataSetSourceFileStore("Datasets"));
builder.Services.AddSingleton<IDataSetSourceStateStore>(
    new FileDataSetSourceStateStore(Path.Combine(Path.GetTempPath(), "ClimateExplorer", "DataSetSourceState")));
builder.Services.AddSingleton(CreateDataSetSourceHttpClient());
builder.Services.AddSingleton<DataSetHttpFileDownloader>();
builder.Services.AddSingleton<IDataSetDownloader, DirectHttpDataSetDownloader>();
builder.Services.AddSingleton<IDataSetDownloader>(
    new GhcndDataSetDownloader(GhcndHttpClientFactory.CreateHttpClient()));
builder.Services.AddSingleton<IDataSetDownloader>(
    new BomDataSetDownloader(new BomDailyDataClient(BomHttpClientFactory.CreateBomHttpClient())));
builder.Services.AddSingleton<IDataSetDownloader>(
    services => new NoaaGlobalTempDataSetDownloader(
        services.GetRequiredService<DataSetHttpFileDownloader>(),
        services.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton<IDataSetDownloader>(
    services => new GreenlandDataSetDownloader(
        new GreenlandMeltDataClient(services.GetRequiredService<HttpClient>()),
        services.GetRequiredService<DataSetSourceFileStore>(),
        services.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton<IDataSetDownloader>(
    services => new TransformingDataSetDownloader(
        "ocean-acidity",
        services.GetRequiredService<DataSetHttpFileDownloader>(),
        new OceanAciditySourceFileTransformer()));
builder.Services.AddSingleton<IDataSetDownloader>(
    services => new TransformingDataSetDownloader(
        "sea-level",
        services.GetRequiredService<DataSetHttpFileDownloader>(),
        new SeaLevelSourceFileTransformer()));
builder.Services.AddSingleton<IDataSetDownloader>(
    services => new TransformingDataSetDownloader(
        "ozone",
        services.GetRequiredService<DataSetHttpFileDownloader>(),
        new OzoneSourceFileTransformer()));
builder.Services.AddSingleton<DataSetSourceUpdateCoordinator>();
builder.Services.AddSingleton<IDataSetSourceUpdateCoordinator>(
    services => services.GetRequiredService<DataSetSourceUpdateCoordinator>());
builder.Services.AddSingleton(
    services => new ClimateExplorerApiServices(
        everydayCache,
        longtermCache,
        BomHttpClientFactory.CreateBomHttpClient(),
        GhcndHttpClientFactory.CreateHttpClient(),
        services.GetRequiredService<IDataSetSourceUpdateCoordinator>(),
        services.GetRequiredService<ILogger<ClimateExplorerApiServices>>()));
builder.Services.AddSingleton(new AcornSatExtensionCache(everydayCache));
builder.Services.AddSingleton<AcornSatClimateRecordService>();

var app = builder.Build();
app.UseCors();

app.MapClimateExplorerEndpoints();

app.Run();

static HttpClient CreateDataSetSourceHttpClient()
{
    var client = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(2),
    };
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ClimateExplorer/1.0 dataset-refresh");
    return client;
}
