namespace ClimateExplorer.WebApi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.WebApi.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static ClimateExplorer.Core.Enums;

internal static class LocationEndpoints
{
    public static async Task<IEnumerable<Location>> GetLocations(
        [FromServices] ClimateExplorerApiServices services,
        Guid? locationId = null,
        bool permitCreateCache = true)
    {
        return await GetCachedLocations(services, locationId, permitCreateCache);
    }

    public static async Task<IEnumerable<Location>> GetCachedLocations(
        ClimateExplorerApiServices services,
        Guid? locationId = null,
        bool permitCreateCache = true)
    {
        string cacheKey = null;
        Location[] cacheResult = null;
        if (locationId == null)
        {
            cacheKey = "Locations";
            cacheResult = await services.LongtermCache.Get<Location[]>(cacheKey);
        }
        else
        {
            cacheKey = $"Locations_{locationId}";
            cacheResult = await services.Cache.Get<Location[]>(cacheKey);
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

        var definitions = await MetadataEndpoints.GetDataSetDefinitions();

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
                            CupSize = ClimateExplorerApiConstants.DefaultCupSize,
                            RequiredBinDataProportion = 1.0f,
                            RequiredBucketDataProportion = 1.0f,
                            RequiredCupDataProportion = ClimateExplorerApiConstants.DefaultCupDataProportion,
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
                    var tempMaxResponse = await ClimateRecordsEndpoints.GetClimateRecords(services, location.Id, DataType.TempMax, adj, take: 1);
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
        await services.LongtermCache.Put(ClimateExplorerApiConstants.HeatingScoreTable, heatingScoreTable.ToArray());

        await services.LongtermCache.Put(cacheKey, locations.ToArray());

        return locations;
    }

    public static async Task<List<LocationDistance>> GetNearbyLocations(
        [FromServices] ClimateExplorerApiServices services,
        Guid locationId,
        int? take = null,
        int? skip = null)
    {
        var cacheKey = $"{ClimateExplorerApiConstants.NearbyLocations}_{locationId}";
        var nearby = await services.Cache.Get<List<LocationDistance>>(cacheKey);

        if (nearby == null)
        {
            var locations = await GetCachedLocations(services);
            var location = locations.Single(x => x.Id == locationId);
            nearby = [.. Location.GetDistances(location, locations).OrderBy(x => x.Distance)];
            await services.Cache.Put(cacheKey, nearby);
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

    public static async Task<Location> GetLocationByPath(
        [FromServices] ClimateExplorerApiServices services,
        string path)
    {
        var locations = await GetCachedLocations(services);

        // There are some duplicate location names (e.g., Jan Mayen and Uliastai)
        // Use FirstOrDefault rather than SingleOrDefault
        var location = locations.FirstOrDefault(x => x.UrlReadyName() == path);
        return location;
    }

    public static async Task<Location> GetLocationById(
        [FromServices] ClimateExplorerApiServices services,
        Guid locationId)
    {
        var locations = await GetCachedLocations(services);
        var location = locations.SingleOrDefault(x => x.Id == locationId);
        return location;
    }

    public static async Task<IResult> GetLocationDataSetMetadata(
        [FromServices] ClimateExplorerApiServices services,
        Guid locationId)
    {
        var metadataService = new LocationDataSetMetadataService(
            getLocations: () => GetCachedLocations(services, locationId, permitCreateCache: false));

        var result = await metadataService.GetAsync(locationId);
        if (!result.LocationFound)
        {
            return Results.NotFound(new
            {
                Message = $"Location {locationId} was not found.",
                LocationId = locationId,
            });
        }

        return Results.Ok(result.SourceMetadata);
    }

    public static List<Region> GetRegions()
    {
        return Region.GetRegions();
    }
}
