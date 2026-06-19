namespace ClimateExplorer.WebApi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApi.RecentObservations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;

internal static class RecentObservationsEndpoints
{
    public static async Task<RecentObservationsResponse> GetRecentObservations(
        [FromServices] ClimateExplorerApiServices services,
        [FromServices] ILoggerFactory loggerFactory,
        Guid locationId,
        DataType dataType = DataType.TempMax,
        bool isLocationSupported = false)
    {
        var logger = loggerFactory.CreateLogger(nameof(RecentObservationsEndpoints));
        var today = DateOnly.FromDateTime(DateTime.Today);
        var yesterday = today.AddDays(-1);
        var context = await GetRecentObservationsContext(services, locationId, dataType, today);
        if (isLocationSupported || context == null)
        {
            return new RecentObservationsResponse { IsSupported = context != null };
        }

        var recentObservationStartDate = new DateOnly(today.Year - 1, 1, 1);
        var recentObservationEndDate = new DateOnly(today.Year, 12, 31);
        var cacheKey = $"{ClimateExplorerApiConstants.RecentObservationsCacheKeyPrefix}_{locationId}_{dataType}_{recentObservationStartDate.Year}_{recentObservationEndDate.Year}";
        var cachedResponse = await services.Cache.Get<RecentObservationsResponse>(cacheKey);
        if (HasRecordForDate(cachedResponse, today) || HasRecordForDate(cachedResponse, yesterday) || WasDataRetrievedInLastHours(cachedResponse, 6))
        {
            return cachedResponse;
        }

        try
        {
            var records = await DownloadAndReadRecentObservationsData(
                services,
                dataType,
                context,
                recentObservationStartDate,
                recentObservationEndDate,
                logger);

            if (records == null)
            {
                return cachedResponse ?? new RecentObservationsResponse { IsSupported = true };
            }

            var response = new RecentObservationsResponse
            {
                RetrievedDate = DateTimeOffset.UtcNow,
                IsSupported = true,
                DataAdjustment = context.MeasurementDefinition.DataAdjustment,
                DataResolution = context.MeasurementDefinition.DataResolution,
                DataType = context.MeasurementDefinition.DataType,
                UnitOfMeasure = context.MeasurementDefinition.UnitOfMeasure,
                Records = records,
            };

            await services.Cache.Put(cacheKey, response);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to retrieve recent {Source} observations for location {LocationId} and data type {DataType}", context.Source, locationId, dataType);
            return cachedResponse ?? new RecentObservationsResponse { IsSupported = true };
        }
    }

    private static async Task<RecentObservationsContext> GetRecentObservationsContext(
        ClimateExplorerApiServices services,
        Guid locationId,
        DataType dataType,
        DateOnly asAt)
    {
        var locations = await LocationEndpoints.GetCachedLocations(services, locationId, permitCreateCache: false);
        var location = locations.SingleOrDefault(x => x.Id == locationId);
        if (location == null)
        {
            return null;
        }

        var dataSetDefinitions = await DataSetDefinition.GetDataSetDefinitions();
        return RecentObservationsBomDataSource.GetContext(location, dataType, asAt, dataSetDefinitions)
            ?? RecentObservationsGhcndDataSource.GetContext(location, dataType, asAt, dataSetDefinitions);
    }

    private static async Task<List<DataRecord>> DownloadAndReadRecentObservationsData(
        ClimateExplorerApiServices services,
        DataType dataType,
        RecentObservationsContext context,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        return context.Source switch
        {
            RecentObservationStationSource.Bom => await RecentObservationsBomDataSource.DownloadAndReadData(
                services.BomHttpClient,
                dataType,
                context,
                startDate,
                endDate,
                logger),
            RecentObservationStationSource.Ghcnd => await RecentObservationsGhcndDataSource.DownloadAndReadData(
                services.GhcndHttpClient,
                dataType,
                context,
                startDate,
                endDate,
                logger),
            _ => null,
        };
    }

    private static bool HasRecordForDate(RecentObservationsResponse response, DateOnly date)
    {
        return response?.Records.Any(x => x.Year == date.Year && x.Month == date.Month && x.Day == date.Day) == true;
    }

    private static bool WasDataRetrievedInLastHours(RecentObservationsResponse response, int hours = 24)
    {
        return response?.RetrievedDate >= DateTimeOffset.UtcNow.AddHours(-1 * hours);
    }
}
