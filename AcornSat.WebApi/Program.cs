using AcornSat.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static AcornSat.Core.Enums;
using System.Threading.Tasks;
using AcornSat.Core.ViewModel;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using System.Reflection;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Calculators;
using AcornSat.WebApi.Infrastructure;

ICache _cache = new FileBackedCache("cache");

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(
    opt =>
    {
//        opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

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
            }
        );
    }
);

builder.Services.Configure<JsonOptions>(
    options =>
    {
        // This causes the JSON returned from API calls to omit properties if their value is null anyway
        options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    }
);

builder.Logging.AddConsole();

var app = builder.Build();
app.UseCors();

app.MapGet(
    "/", () =>
        "Hello, from minimal ACORN-SAT Web API!\n" +
        "\n" +
        "Operations:\n" +
        "   GET /about\n" +
        "       Returns basic API metadata\n" +
        "   GET /datasetdefinition\n" +
        "       Returns a list of dataset definitions. (E.g., ACORN-SAT)\n" +
        "   GET /location\n" +
        "       Returns a list of locations.\n" +
        "           Parameters:\n" +
        "               locationId: filter to a particular location by id (still returns an array of location, but max one entry)\n" +
        "   POST /dataset\n" +
        "       Returns the specified data set, transformed as requested");

app.MapGet("/about",                                                GetAbout);
app.MapGet("/datasetdefinition",                                    GetDataSetDefinitions);
app.MapGet("/location",                                             GetLocations);
app.MapPost("/dataset",                                             PostDataSets);

app.Run();

object GetAbout()
{
    var asm = Assembly.GetExecutingAssembly();

    return
        new
        {
            Version = asm.GetName().Version.ToString(),
            BuildTimeUtc = File.GetLastWriteTimeUtc(asm.Location)
        };
}

async Task<List<DataSetDefinitionViewModel>> GetDataSetDefinitions()
{
    var definitions = await DataSetDefinition.GetDataSetDefinitions();

    var dtos =
        definitions
        .Select(
            async x =>
            new DataSetDefinitionViewModel
            {
                Id = x.Id,
                Name = x.Name,
                MoreInformationUrl = x.MoreInformationUrl,
                StationInfoUrl = x.StationInfoUrl,
                LocationInfoUrl = x.LocationInfoUrl,
                Description = x.Description,
                LocationIds = x.DataLocationMapping?.LocationIdToDataFileMappings.Keys.ToList(),
                MeasurementDefinitions = x.MeasurementDefinitions.Select(x => x.ToViewModel()).ToList(),
            })
        .Select(x => x.Result)
        .ToList();

    return dtos;
}

async Task<IEnumerable<Location>> GetLocations(string locationId = null, bool includeNearbyLocations = false, bool includeWarmingMetrics = false)
{
    string cacheKey = $"Locations_{locationId}_{includeNearbyLocations}_{includeWarmingMetrics}";

    var result = await _cache.Get<Location[]>(cacheKey);

    if (result != null) return result;

    IEnumerable<Location> locations = (await Location.GetLocations(includeNearbyLocations)).OrderBy(x => x.Name);
    
    if (locationId != null)
    {
        locations = locations.Where(x => x.Id == Guid.Parse(locationId));
    }

    locations = locations.ToList();

    if (includeWarmingMetrics)
    {
        var definitions = await GetDataSetDefinitions();

        // For each location, retrieve the TempMax dataset (Adjusted if available, Adjustment null otherwise), and copy its WarmingIndex
        // to the location we're about to return.
        foreach (var location in locations)
        {
            try
            {
                // First, find what data adjustments are available for TempMax for that location.
                var dsdmd =
                    DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
                        definitions,
                        location.Id,
                        DataType.TempMax,
                        Enums.DataAdjustment.Adjusted,
                        true);

                // Next, request that dataset
                var dsb = new DataSetBuilder();

                var series =
                    await dsb.BuildDataSet(
                        new PostDataSetsRequestBody
                        {
                            BinAggregationFunction = ContainerAggregationFunctions.Mean,
                            BinningRule = BinGranularities.ByYear,
                            BucketAggregationFunction = ContainerAggregationFunctions.Mean,
                            CupAggregationFunction = ContainerAggregationFunctions.Mean,
                            CupSize = 14,
                            RequiredBinDataProportion = 1.0f,
                            RequiredBucketDataProportion = 1.0f,
                            RequiredCupDataProportion = 0.7f,
                            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                            SeriesSpecifications =
                                new SeriesSpecification[]
                                {
                                    new SeriesSpecification
                                    {
                                        DataAdjustment = dsdmd.MeasurementDefinition.DataAdjustment,
                                        DataSetDefinitionId = dsdmd.DataSetDefinition.Id,
                                        DataType = dsdmd.MeasurementDefinition.DataType,
                                        LocationId = location.Id
                                    }
                                }
                        }
                    );

                location.WarmingIndex = WarmingIndexCalculator.CalculateWarmingIndex(series.DataPoints)?.WarmingIndexValue;
            }
            catch (Exception ex)
            {
                // For now, just swallow failures here, caused by missing data
                Console.WriteLine("Exception while calculating warming index for " + location.Name + ": " + ex.ToString());
            }
        }

        // heatingScore is calculated across the full set of locations. If we've been asked for details on just one location, we don't calculate it.
        if (locationId == null)
        {
            var maxWarmingIndex = locations.Max(x => x.WarmingIndex).Value;

            locations
                .ToList()
                .ForEach(x => x.HeatingScore = x.WarmingIndex == null
                                                    ? null
                                                    : x.WarmingIndex > 0
                                                            ? Convert.ToInt16(MathF.Round(x.WarmingIndex.Value / maxWarmingIndex * 9, 0))
                                                            : Convert.ToInt16(MathF.Round(x.WarmingIndex.Value, 0)));
        }
    }

    await _cache.Put<Location[]>(cacheKey, locations.ToArray());

    return locations;
}

async Task<DataSet> PostDataSets(PostDataSetsRequestBody body)
{
    var dsb = new DataSetBuilder();

    var series = await dsb.BuildDataSet(body);

    var definitions = await DataSetDefinition.GetDataSetDefinitions();
    var spec = body.SeriesSpecifications[0];
    var dsd = definitions.Single(x => x.Id == spec.DataSetDefinitionId);

    Location location =
        spec.LocationId != null
        ? (await Location.GetLocations(false)).Single(x => x.Id == spec.LocationId)
        : null;

    var returnDataSet =
        new DataSet
        {
            Location = location,
            Resolution = DataResolution.Yearly,
            MeasurementDefinition = 
                new MeasurementDefinitionViewModel 
                { 
                    DataAdjustment = spec.DataAdjustment, 
                    DataType = spec.DataType.Value,
                    UnitOfMeasure = series.UnitOfMeasure,
                    DataCategory = series.DataCategory
                },
            DataRecords = 
                series.DataPoints
                .Select(
                    x => 
                    new DataRecord 
                    { 
                        Label = x.Label, 
                        Value = x.Value, 
                        BinId = x.BinId,
                    }
                )
                .ToList(),
            RawDataRecords =
                body.IncludeRawDataPoints
                ? series.RawDataPoints
                : null

        };

    return returnDataSet;

}