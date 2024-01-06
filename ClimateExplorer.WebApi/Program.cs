using ClimateExplorer.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ClimateExplorer.Core.Enums;
using System.Threading.Tasks;
using ClimateExplorer.Core.ViewModel;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using System.Reflection;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.WebApi.Infrastructure;
using System.Text.Json;
using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.DataPreparation.DataSetBuilder;
using System.Text.Json.Serialization;

//ICache _cache = new FileBackedCache("cache");
ICache _cache = new FileBackedTwoLayerCache("cache");

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
            }
        );
    }
);

builder.Services.Configure<JsonOptions>(
    options =>
    {
        // This causes the JSON returned from API calls to omit properties if their value is null anyway
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }
);

builder.Logging.AddConsole();

var app = builder.Build();
app.UseCors();

app.MapGet(
    "/", () =>
        "Hello, from minimal Climate Explorer Web API!\n" +
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
        "   GET /location-by-path\n" +
        "       Returns a single location given it's path ready name\n" +
        "           Parameters:\n" +
        "               path: name of the location that has been passed to the web client. For example: hobart, cape-town-intl, birni-n-konni. These are paths in the sitemap.xml\n" +
        "   GET /country\n" +
        "       Returns a list of countries.\n" +
        "   GET /region\n" +
        "       Returns a list of regions.\n" +
        "   POST /dataset\n" +
        "       Returns the specified data set, transformed as requested");

app.MapGet("/about",                                                GetAbout);
app.MapGet("/datasetdefinition",                                    GetDataSetDefinitions);
app.MapGet("/location",                                             GetLocations);
app.MapGet("/location-by-path",                                     GetLocationByPath);
app.MapGet("/country",                                              GetCountries);
app.MapGet("/region",                                               GetRegions);
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
            x =>
            new DataSetDefinitionViewModel
            {
                Id = x.Id,
                Name = x.Name,
                MoreInformationUrl = x.MoreInformationUrl,
                StationInfoUrl = x.StationInfoUrl,
                LocationInfoUrl = x.LocationInfoUrl,
                Description = x.Description,
                Publisher = x.Publisher,
                PublisherUrl = x.PublisherUrl,
                LocationIds = x.DataLocationMapping?.LocationIdToDataFileMappings.Keys.ToList(),
                MeasurementDefinitions = x.MeasurementDefinitions.Select(x => x.ToViewModel()).ToList(),
            })
        .ToList();

    return dtos;
}

async Task<IEnumerable<Location>> GetLocations(Guid? locationId = null)
{
    return await GetCachedLocations(locationId);
}

async Task<IEnumerable<Location>> GetCachedLocations(Guid? locationId = null, bool includeNearbyLocations = false)
{
    // If we're asking for a single location, we'll always return nearby locations
    if (locationId != null)
    {
        includeNearbyLocations = true;
    }

    string cacheKey = $"Locations_{locationId}_{includeNearbyLocations}";

    var result = await _cache.Get<Location[]>(cacheKey);
    if (result != null)
    {
        return result;
    }

    IEnumerable<Location> locations;

    if (locationId != null)
    {
        var allLocations = (await GetCachedLocations(includeNearbyLocations: true)).OrderBy(x => x.Name);
        locations = allLocations.Where(x => x.Id == locationId);
    }
    else
    {
        locations = (await Location.GetLocations()).OrderBy(x => x.Name);

        var definitions = await GetDataSetDefinitions();

        // For each location, retrieve the TempMax dataset (Adjusted if available, Adjustment null otherwise), and copy its WarmingAnomaly
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
                        DataSubstitute.StandardTemperatureDataMatches(),
                        throwIfNoMatch: true);

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
                                    new() {
                                        DataAdjustment = dsdmd.MeasurementDefinition.DataAdjustment,
                                        DataSetDefinitionId = dsdmd.DataSetDefinition.Id,
                                        DataType = dsdmd.MeasurementDefinition.DataType,
                                        LocationId = location.Id
                                    }
                                }
                        }
                    );

                location.WarmingAnomaly = AnomalyCalculator.CalculateAnomaly(series.DataPoints)?.AnomalyValue;
            }
            catch (Exception ex)
            {
                // For now, just swallow failures here, caused by missing data
                Console.WriteLine("Exception while calculating warming index for " + location.Name + ": " + ex.ToString());
            }
        }

        Location.SetHeatingScores(locations);
        if (includeNearbyLocations)
        {
            Location.SetNearbyLocations(locations);
        }
    }

    await _cache.Put(cacheKey, locations.ToArray());

    return locations;
}

async Task<DataSet> PostDataSets(PostDataSetsRequestBody body)
{
    string cacheKey = $"DataSet_" + JsonSerializer.Serialize(body);

    var result = await _cache.Get<DataSet>(cacheKey);

    if (result != null) return result;

    var dsb = new DataSetBuilder();

    BuildDataSetResult series = null;

    if (body.SeriesDerivationType == SeriesDerivationTypes.AverageOfAnomaliesInRegion)
    {
        var regionId = body.SeriesSpecifications[0].LocationId;
        var region = Region.GetRegion(regionId);

        var anomalyDatasets = new List<DataSet>();

        // The following section is probably the most expensive operation in the whole application
        // Let's do it all parallel baby, like we did in 2007!
        ParallelOptions parallelOptions = new();
        await Parallel.ForEachAsync(region.LocationIds, parallelOptions, async (locationId, cancellationToken) =>
        {
            // Initial values will be absolute values
            body.Anomaly = false;
            var dataset = await PostDataSets(GetPostRequestBody(body, locationId));
            var anomalyDataSet = GenerateAnomalyDataSetForLocation(dataset);

            if (anomalyDataSet != null)
            {
                anomalyDatasets.Add(anomalyDataSet);
            }
        });

        // We want to always return an anomalous result
        body.Anomaly = true;
        series = await GenerateAverageOfAnomaliesSeries(body, series, anomalyDatasets);
    }
    else
    {
        series = await dsb.BuildDataSet(body);
    }
    
    var definitions = await DataSetDefinition.GetDataSetDefinitions();
    var spec = body.SeriesSpecifications[0];
    var dsd = definitions.Single(x => x.Id == spec.DataSetDefinitionId);

    var geoEntity = await GeographicalEntity.GetGeographicalEntity(spec.LocationId);

    var returnDataSet =
        new DataSet
        {
            GeographicalEntity = geoEntity,
            Resolution = DataResolution.Yearly,
            MeasurementDefinition = 
                new MeasurementDefinitionViewModel 
                { 
                    DataAdjustment = spec.DataAdjustment, 
                    DataType = spec.DataType,
                    UnitOfMeasure = series.UnitOfMeasure,
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

    // If the BinningRule is ByYearAndDay then there is little to gain by caching the data
    // because we haven't done any aggregation. Therefore, return early, before the cache step
    if (body.BinningRule == BinGranularities.ByYearAndDay)
    {
        return returnDataSet;
    }
    await _cache.Put(cacheKey, returnDataSet);
    return returnDataSet;
}

static PostDataSetsRequestBody GetPostRequestBody(PostDataSetsRequestBody body, Guid locationId)
{
    return new PostDataSetsRequestBody
    {

        BinAggregationFunction = body.BinAggregationFunction,
        BucketAggregationFunction = body.BucketAggregationFunction,
        CupAggregationFunction = body.CupAggregationFunction,
        BinningRule = body.BinningRule,
        CupSize = body.CupSize,
        RequiredBinDataProportion = body.RequiredBinDataProportion,
        RequiredBucketDataProportion = body.RequiredBinDataProportion,
        RequiredCupDataProportion = body.RequiredCupDataProportion,
        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
        SeriesSpecifications = new SeriesSpecification[]
                    {
                    new SeriesSpecification
                    {
                        DataAdjustment = body.SeriesSpecifications[0].DataAdjustment,
                        DataSetDefinitionId = body.SeriesSpecifications[0].DataSetDefinitionId,
                        DataType = body.SeriesSpecifications[0].DataType,
                        LocationId = locationId,
                    },
                    },
        SeriesTransformation = body.SeriesTransformation,
        Anomaly = body.Anomaly,
        FilterToYear = body.FilterToYear,
    };
}

// These are constants for now but it would be great to them into the UI to allow the user to be able to adjust the reference period and the threshold.
// Currently, for Australia, Burketown, Eucla, Learmonth, Morawa, Robe, Snowtown, and Victoria River Downs are excluded from the analysis because they don't have enough records in the reference period.
const short ReferenceStartYear = 1961;
const short ReferenceEndYear = 1990;
const float ReferencePeriodThreshold = 0.5f;

static DataSet GenerateAnomalyDataSetForLocation(DataSet dataset)
{
    dataset.DataRecords.ForEach(x => x.Year = ((YearBinIdentifier)BinIdentifier.Parse(x.BinId)).Year);

    float referencePeriod = ReferenceEndYear - ReferenceStartYear + 1;

    var referencePeriodCount = dataset.Years.Count(x => x >= ReferenceStartYear && x <= ReferenceEndYear);
    if (referencePeriodCount / referencePeriod > ReferencePeriodThreshold)
    {
        var referencePeriodAverage = dataset.DataRecords.Where(x => x.Year >= 1961 && x.Year <= 1990).Average(x => x.Value);
        var anomalyRecords = new List<DataRecord>();
        foreach (var record in dataset.DataRecords)
        {
            anomalyRecords.Add(new DataRecord
            {
                Label = record.Label,
                BinId = record.BinId,
                Value = record.Value - referencePeriodAverage,
                Year = record.Year,
            });
        }
        return
            new DataSet
            {
                DataRecords = anomalyRecords
            };
    }

    Console.WriteLine($"There are only {referencePeriodCount} records for this dataset ({dataset.GeographicalEntity.Name}) in the reference period ({ReferenceStartYear}-{ReferenceEndYear}). A minimum of {ReferencePeriodThreshold * referencePeriod} records ({Math.Round(ReferencePeriodThreshold * 100, 0)}%) for the reference period are required. {dataset.GeographicalEntity.Name} will be excluded from the analysis.");
    return null;
}

static async Task<BuildDataSetResult> GenerateAverageOfAnomaliesSeries(PostDataSetsRequestBody body, BuildDataSetResult series, List<DataSet> anomalyDatasets)
{
    var minYear = (short)anomalyDatasets.Min(x => x.StartYear);

    var dataPoints = new List<ChartableDataPoint>();
    for (var i = minYear; i <= DateTime.Now.Year; i++)
    {
        var averageForYear = anomalyDatasets.Average(x => x.DataRecords.SingleOrDefault(y => y.Year == i)?.Value);
        dataPoints.Add(new ChartableDataPoint
        {
            BinId = $"y{i}",
            Label = i.ToString(),
            Value = averageForYear
        });
    }
    var seriesDefinition = await SeriesProvider.GetSeriesDataPointsForRequest(body.SeriesDerivationType, body.SeriesSpecifications);
    series = new BuildDataSetResult
    {
        DataPoints = dataPoints.ToArray(),
        UnitOfMeasure = seriesDefinition.UnitOfMeasure,
    };
    return series;
}

async Task<Dictionary<string, string>> GetCountries()
{
    return (await Country.GetCountries(@"MetaData\countries.txt")).ToDictionary(x => x.Key, x => x.Value.Name);
}

List<Region> GetRegions()
{
    return Region.GetRegions();
}

async Task<Location> GetLocationByPath(string path)
{
    var locations = await GetCachedLocations();

    // There are some duplicate location names (e.g., Jan Mayen and Uliastai)
    // Use FirstOrDefault rather than SingleOrDefault
    var location = locations.FirstOrDefault(x => x.UrlReadyName() == path);
    return location;
}