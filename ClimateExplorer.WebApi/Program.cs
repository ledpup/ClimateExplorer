#pragma warning disable SA1200 // Using directives should be placed correctly
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.WebApi.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;
#pragma warning restore SA1200 // Using directives should be placed correctly

ICache cache = new FileBackedTwoLayerCache("cache");
ICache longtermCache = new FileBackedTwoLayerCache("cache-longterm");

const string HeatingScoreTable = "HeatingScoreTable";
const string NearbyLocations = "NearbyLocations";

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
        "   GET /heating-score-table\n" +
        "       A table that records the range of warming anomalies for each heating score.\n" +
        "   GET /climate-record\n" +
        "       Returns a list of climate-records at a specific location\n" +
        "           Parameters:\n" +
        "               locationId: location id for the climate-records\n" +
        "   POST /dataset\n" +
        "       Returns the specified data set, transformed as requested");

app.MapGet("/about", GetAbout);
app.MapGet("/datasetdefinition", GetDataSetDefinitions);
app.MapGet("/location", GetLocations);
app.MapGet("/location-by-path", GetLocationByPath);
app.MapGet("/nearby-locations", GetNearbyLocations);
app.MapGet("/country", GetCountries);
app.MapGet("/region", GetRegions);
app.MapGet("/heating-score-table", GetHeatingScoreTable);
app.MapGet("/climate-record", GetClimateRecords);
app.MapPost("/dataset", PostDataSets);

app.Run();

object GetAbout()
{
    var asm = Assembly.GetExecutingAssembly();

    return
        new
        {
            Version = asm.GetName().Version.ToString(),
            BuildTimeUtc = File.GetLastWriteTimeUtc(asm.Location),
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
                LocationIds = x.DataLocationMapping?.LocationIdToDataFileMappings.Keys.ToHashSet(),
                MeasurementDefinitions = x.MeasurementDefinitions.Select(x => x.ToViewModel()).ToList(),
            })
        .ToList();

    return dtos;
}

async Task<IEnumerable<Location>> GetLocations(Guid? locationId = null, bool permitCreateCache = true)
{
    return await GetCachedLocations(locationId, permitCreateCache);
}

async Task<IEnumerable<Location>> GetCachedLocations(Guid? locationId = null, bool permitCreateCache = true)
{
    string cacheKey = null;
    Location[] cacheResult = null;
    if (locationId == null)
    {
        cacheKey = "Locations";
        cacheResult = await longtermCache.Get<Location[]>(cacheKey);
    }
    else
    {
        cacheKey = $"Locations_{locationId}";
        cacheResult = await cache.Get<Location[]>(cacheKey);
    }

    if (cacheResult != null)
    {
        return cacheResult;
    }

    IEnumerable<Location> locations;

    locations = (await Location.GetLocations()).OrderBy(x => x.Name);

    if (!permitCreateCache)
    {
        return locations;
    }

    var definitions = await GetDataSetDefinitions();

    ParallelOptions parallelOptions = new ();

    // For each location, retrieve the TempMean dataset (Adjusted if available, Adjustment null otherwise), and copy its WarmingAnomaly
    // to the location we're about to return.
    await Parallel.ForEachAsync(locations!, parallelOptions, async (location, token) =>
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
                            [
                                new ()
                                {
                                    DataAdjustment = dsdmd.MeasurementDefinition.DataAdjustment,
                                    DataSetDefinitionId = dsdmd.DataSetDefinition.Id,
                                    DataType = dsdmd.MeasurementDefinition.DataType,
                                    LocationId = location.Id,
                                },
                            ],
                    });

            location.WarmingAnomaly = AnomalyCalculator.CalculateAnomaly(series.DataPoints)?.AnomalyValue;
            var climateRecords = await GetClimateRecords(location.Id);
            var max = climateRecords.Where(x => x.DataType == DataType.TempMax && x.RecordType == RecordType.High);
            if (max.Count() > 0)
            {
                if (max.Count() > 1)
                {
                    max = max.Where(x => x.DataAdjustment == DataAdjustment.Adjusted);
                }

                location.RecordHigh = max.Single();
            }
        }
        catch (Exception ex)
        {
            // For now, just swallow failures here, caused by missing data
            Console.WriteLine("Exception while calculating warming index for " + location.Name + ": " + ex.ToString());
        }
    });

    // Can't set the heating scores until all warming anomalies are calculated.
    var heatingScoreTable = Location.SetHeatingScores(locations);
    await longtermCache.Put(HeatingScoreTable, heatingScoreTable.ToArray());

    var nearbyLocations = Location.GenerateNearbyLocations(locations);
    await longtermCache.Put(NearbyLocations, nearbyLocations);

    await longtermCache.Put(cacheKey, locations.ToArray());

    return locations;
}

async Task<List<LocationDistance>> GetNearbyLocations(Guid locationId)
{
    var nearbyLocations = await longtermCache.Get<Dictionary<Guid, List<LocationDistance>>>(NearbyLocations);
    return nearbyLocations[locationId];
}

async Task<DataSet> PostDataSets(PostDataSetsRequestBody body)
{
    string cacheKey = $"DataSet_" + JsonSerializer.Serialize(body);

    var result = await cache.Get<DataSet>(cacheKey);

    if (result != null)
    {
        return result;
    }

    var dsb = new DataSetBuilder();

    var series = await dsb.BuildDataSet(body);
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
                .Select(x => new BinnedRecord(x.BinId, x.Value.HasValue ? Math.Round(x.Value.Value, 4) : null)) // 4 decimal places should be enough for anyone
                .ToList(),
            RawDataRecords =
                body.IncludeRawDataRecords
                ? series.RawDataRecords
                : null,
        };

    // If the BinningRule is ByYearAndDay then there is little to gain by caching the data
    // because we haven't done any aggregation. Therefore, return early, before the cache step
    if (body.BinningRule == BinGranularities.ByYearAndDay)
    {
        return returnDataSet;
    }

    await cache.Put(cacheKey, returnDataSet);
    return returnDataSet;
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

async Task<IEnumerable<HeatingScoreRow>> GetHeatingScoreTable()
{
    var result = await longtermCache.Get<List<HeatingScoreRow>>(HeatingScoreTable);
    return result;
}

async Task<IEnumerable<ClimateRecord>> GetClimateRecords(Guid locationId)
{
    string cacheKey = $"ClimateRecord_" + JsonSerializer.Serialize(locationId);
    var result = await longtermCache.Get<IEnumerable<ClimateRecord>>(cacheKey);
    if (result != null)
    {
        return result;
    }

    var dataSetDefinitions = await GetDataSetDefinitions();
    var locationDsds = dataSetDefinitions.Where(x => x.LocationIds.Contains(locationId));

    var climateRecords = new List<ClimateRecord>();

    foreach (var dataSetDefinition in locationDsds)
    {
        foreach (var md in dataSetDefinition.MeasurementDefinitions)
        {
            if (!(md.DataResolution == DataResolution.Daily || md.DataResolution == DataResolution.Monthly))
            {
                throw new NotImplementedException();
            }

            var dataSets = await PostDataSets(
                new PostDataSetsRequestBody
                {
                    BinningRule = md.DataResolution == DataResolution.Daily ? BinGranularities.ByYearAndDay : BinGranularities.ByYearAndMonth,
                    SeriesTransformation = SeriesTransformations.Identity,
                    SeriesSpecifications =
                    [
                        new SeriesSpecification
                        {
                            DataSetDefinitionId = dataSetDefinition.Id,
                            DataType = md.DataType,
                            LocationId = locationId,
                            DataAdjustment = md.DataAdjustment,
                        },
                    ],
                });

            climateRecords.Add(CreateClimateRecord(md, dataSets, RecordType.High));
            climateRecords.Add(CreateClimateRecord(md, dataSets, RecordType.Low));
        }
    }

    await longtermCache.Put(cacheKey, climateRecords);

    return climateRecords;
}

static ClimateRecord CreateClimateRecord(MeasurementDefinitionViewModel md, DataSet dataSets, RecordType recordType)
{
    double record = (double)(recordType == RecordType.High ? dataSets.DataRecords.Max(x => x.Value)
                                                           : dataSets.DataRecords.Min(x => x.Value));
    var records = dataSets.DataRecords.Where(x => x.Value == record).OrderBy(x => x.BinId).ToList();
    var binId = records.FirstOrDefault().BinIdentifier;

    var cr = new ClimateRecord
    {
        DataAdjustment = md.DataAdjustment,
        DataResolution = md.DataResolution,
        DataType = md.DataType,
        UnitOfMeasure = md.UnitOfMeasure,
        RecordType = recordType,
        Value = record,
        NumberOfTimes = records.Count,
    };

    switch (md.DataResolution)
    {
        case DataResolution.Daily:
            {
                var bin = (YearAndDayBinIdentifier)binId;
                cr.Year = bin.Year;
                cr.Month = bin.Month;
                cr.Day = bin.Day;
                break;
            }

        case DataResolution.Monthly:
            {
                var bin = (YearAndMonthBinIdentifier)binId;
                cr.Year = bin.Year;
                cr.Month = bin.Month;
                break;
            }

        default:
            throw new NotImplementedException();
    }

    return cr;
}