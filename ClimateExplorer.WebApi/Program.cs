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
const float DefaultCupDataProportion = 0.7f;
const int DefaultCupSize = 1;

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
        "       Returns a ranked list of climate records at a specific location\n" +
        "           Parameters:\n" +
        "               locationId: location id for the climate records\n" +
        "               dataType: data type to return records for (default: TempMax)\n" +
        "               dataAdjustment: data adjustment to filter by (optional; omit for data types with no adjustment concept)\n" +
        "               ascending: if true returns lowest values first; if false returns highest values first (default: false)\n" +
        "               count: number of records to return (default: 10)\n" +
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

    ParallelOptions parallelOptions = new();

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
                        CupSize = DefaultCupSize,
                        RequiredBinDataProportion = 1.0f,
                        RequiredBucketDataProportion = 1.0f,
                        RequiredCupDataProportion = DefaultCupDataProportion,
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        SeriesSpecifications =
                            [
                                new()
                                {
                                    DataAdjustment = dsdmd.MeasurementDefinition.DataAdjustment,
                                    DataSetDefinitionId = dsdmd.DataSetDefinition.Id,
                                    DataType = dsdmd.MeasurementDefinition.DataType,
                                    LocationId = location.Id,
                                },
                            ],
                    });

            location.WarmingAnomaly = AnomalyCalculator.CalculateAnomaly(series.DataPoints)?.AnomalyValue;
            foreach (var adj in new DataAdjustment?[] { DataAdjustment.Adjusted, DataAdjustment.Unadjusted, null })
            {
                var tempMaxResponse = await GetClimateRecords(location.Id, DataType.TempMax, adj, take: 1);
                if (tempMaxResponse.Records.Any())
                {
                    location.RecordHigh = tempMaxResponse.Records.First();
                    break;
                }
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

    await longtermCache.Put(cacheKey, locations.ToArray());

    return locations;
}

async Task<List<LocationDistance>> GetNearbyLocations(Guid locationId, int? take = null, int? skip = null)
{
    var cacheKey = $"{NearbyLocations}_{locationId}";
    var nearby = await cache.Get<List<LocationDistance>>(cacheKey);

    if (nearby == null)
    {
        var locations = await GetCachedLocations();
        var location = locations.Single(x => x.Id == locationId);
        nearby = [.. Location.GetDistances(location, locations).OrderBy(x => x.Distance)];
        await cache.Put(cacheKey, nearby);
    }

    IEnumerable<LocationDistance> result = nearby;
    if (skip.HasValue)
    {
        result = result.Skip(skip.Value);
    }

    if (take.HasValue)
    {
        result = result.Take(take.Value);
    }

    return [.. result];
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
                body.IncludeRawDataRecords == true
                ? series.RawDataRecords
                : null,
        };

    // If the BinningRule is ByYearAndDay (or ByDayOnly filtered to a specific year) then there is little
    // to gain by caching the data because we haven't done any aggregation. Therefore, return early, before the cache step
    if (body.BinningRule == BinGranularities.ByYearAndDay ||
        (body.BinningRule == BinGranularities.ByDayOnly && body.FilterToYear.HasValue))
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

async Task<ClimateRecordsResponse> GetClimateRecords(
    Guid locationId,
    DataType dataType = DataType.TempMax,
    DataAdjustment? dataAdjustment = null,
    bool ascending = false,
    int? take = null,
    int? skip = null,
    int? month = null,
    bool monthly = false)
{
    var dataSetDefinitions = await GetDataSetDefinitions();
    var locationDsds = dataSetDefinitions.Where(x => x.LocationIds!.Contains(locationId)).ToList();

    MeasurementDefinitionViewModel md = null;
    DataSetDefinitionViewModel matchingDsd = null;

    var suitableResolutions = monthly ? [DataResolution.Daily, DataResolution.Monthly] : new[] { DataResolution.Daily };
    foreach (var resolution in suitableResolutions)
    {
        foreach (var dsd in locationDsds)
        {
            md = dsd.MeasurementDefinitions?.FirstOrDefault(m =>
                m.DataType == dataType &&
                m.DataAdjustment == dataAdjustment &&
                m.DataResolution == resolution);
            if (md != null)
            {
                matchingDsd = dsd;
                break;
            }
        }

        if (matchingDsd != null)
        {
            break;
        }
    }

    if (md == null || matchingDsd == null)
    {
        return new ClimateRecordsResponse();
    }

    var fn = dataType == DataType.Precipitation ? ContainerAggregationFunctions.Sum : ContainerAggregationFunctions.Mean;
    var dataSet = await PostDataSets(
        new PostDataSetsRequestBody
        {
            SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
            BinningRule = md.DataResolution == DataResolution.Daily && !monthly ? BinGranularities.ByYearAndDay : BinGranularities.ByYearAndMonth,
            SeriesTransformation = SeriesTransformations.Identity,
            SeriesSpecifications =
            [
                new SeriesSpecification
                {
                    DataSetDefinitionId = matchingDsd.Id,
                    DataType = dataType,
                    LocationId = locationId,
                    DataAdjustment = dataAdjustment,
                },
            ],
            BinAggregationFunction = fn,
            BucketAggregationFunction = fn,
            CupAggregationFunction = fn,
            RequiredBinDataProportion = 1,
            RequiredBucketDataProportion = 1,
            RequiredCupDataProportion = DefaultCupDataProportion,
            CupSize = DefaultCupSize,
        });

    static int YearOf(BinnedRecord r) => r.BinIdentifier switch
    {
        YearAndDayBinIdentifier d => d.Year,
        YearAndMonthBinIdentifier m => m.Year,
        _ => 0,
    };

    var allRecords = dataSet.DataRecords.Where(x => x.Value.HasValue).ToList();
    int? startYear = allRecords.Count > 0 ? allRecords.Min(YearOf) : null;
    int? endYear = allRecords.Count > 0 ? allRecords.Max(YearOf) : null;

    var records = allRecords.AsEnumerable();
    if (month.HasValue)
    {
        records = records.Where(x => x.BinIdentifier switch
        {
            YearAndDayBinIdentifier d => d.Month == month.Value,
            YearAndMonthBinIdentifier m => m.Month == month.Value,
            _ => true,
        });
    }

    var ordered = ascending
        ? records.OrderBy(x => x.Value)
        : records.OrderByDescending(x => x.Value);

    var totalCount = ordered.Count();

    // Apply pagination if count and/or page is specified
    IEnumerable<BinnedRecord> paginated = ordered;
    if (take.HasValue)
    {
        if (skip.HasValue && skip.Value > 1)
        {
            paginated = paginated.Skip((skip.Value - 1) * take.Value);
        }

        paginated = paginated.Take(take.Value);
    }

    var climateRecords = paginated.Select(record =>
    {
        var cr = new ClimateRecord
        {
            DataAdjustment = dataAdjustment,
            DataResolution = md.DataResolution,
            DataType = dataType,
            UnitOfMeasure = md.UnitOfMeasure,
            Value = record.Value!.Value,
        };

        if (md.DataResolution == DataResolution.Daily && !monthly)
        {
            var dayBin = (YearAndDayBinIdentifier)record.BinIdentifier!;
            cr.Year = dayBin.Year;
            cr.Month = dayBin.Month;
            cr.Day = dayBin.Day;
        }
        else
        {
            var monthBin = (YearAndMonthBinIdentifier)record.BinIdentifier!;
            cr.Year = monthBin.Year;
            cr.Month = monthBin.Month;
        }

        return cr;
    }).ToList();

    var response = new ClimateRecordsResponse
    {
        Records = climateRecords,
        StartYear = startYear,
        EndYear = endYear,
        TotalCount = totalCount,
    };
    return response;
}
