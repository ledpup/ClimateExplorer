namespace ClimateExplorer.WebApi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApi.RecentObservations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

internal static class RecentObservationsEndpoints
{
    public static async Task<RecentObservationsResponse> GetRecentObservations(
        [FromServices] ClimateExplorerApiServices services,
        [FromServices] ILoggerFactory loggerFactory,
        Guid locationId,
        bool isLocationSupported = false)
    {
        var logger = loggerFactory.CreateLogger(nameof(RecentObservationsEndpoints));
        var today = DateOnly.FromDateTime(DateTime.Today);
        var yesterday = today.AddDays(-1);
        var context = await GetRecentObservationsContext(services, locationId, today);
        if (isLocationSupported || context == null)
        {
            return new RecentObservationsResponse { IsSupported = context?.IsTemperatureSupported ?? false };
        }

        var recentObservationStartDate = new DateOnly(today.Year - 1, 1, 1);
        var recentObservationEndDate = new DateOnly(today.Year, 12, 31);
        var cacheKey = $"{ClimateExplorerApiConstants.RecentObservationsCacheKeyPrefix}_{locationId}";
        var cachedResponse = await services.Cache.Get<RecentObservationsResponse>(cacheKey);
        if (HasSourceMetadata(cachedResponse)
            && (HasRecordForDate(cachedResponse, today) || HasRecordForDate(cachedResponse, yesterday) || WasDataRetrievedInLastHours(cachedResponse, 6)))
        {
            return cachedResponse;
        }

        try
        {
            var downloadResult = await DownloadAndReadRecentObservationsData(
                services,
                context,
                recentObservationStartDate,
                recentObservationEndDate,
                logger);

            if (downloadResult == null)
            {
                return cachedResponse ?? new RecentObservationsResponse { IsSupported = context.IsTemperatureSupported };
            }

            var retrievedAtUtc = DateTimeOffset.UtcNow;
            var response = new RecentObservationsResponse
            {
                RetrievedDate = retrievedAtUtc,
                IsSupported = context.IsTemperatureSupported,
                TempMax = CreateSeries(context.Source, downloadResult.TempMax, retrievedAtUtc),
                TempMin = CreateSeries(context.Source, downloadResult.TempMin, retrievedAtUtc),
                Precipitation = CreateSeries(context.Source, downloadResult.Precipitation, retrievedAtUtc),
            };

            await services.Cache.Put(cacheKey, response);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to retrieve recent {Source} observations for location {LocationId}", context.Source, locationId);
            return cachedResponse ?? new RecentObservationsResponse { IsSupported = context.IsTemperatureSupported };
        }
    }

    private static async Task<RecentObservationsContext> GetRecentObservationsContext(
        ClimateExplorerApiServices services,
        Guid locationId,
        DateOnly asAt)
    {
        var locations = await LocationEndpoints.GetCachedLocations(services, locationId, permitCreateCache: false);
        var location = locations.SingleOrDefault(x => x.Id == locationId);
        if (location == null)
        {
            return null;
        }

        var dataSetDefinitions = await DataSetDefinition.GetDataSetDefinitions();
        return RecentObservationsBomDataSource.GetContext(location, asAt, dataSetDefinitions)
            ?? RecentObservationsGhcndDataSource.GetContext(location, asAt, dataSetDefinitions);
    }

    private static async Task<RecentObservationsDownloadResult> DownloadAndReadRecentObservationsData(
        ClimateExplorerApiServices services,
        RecentObservationsContext context,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        return context.Source switch
        {
            RecentObservationStationSource.Bom => await RecentObservationsBomDataSource.DownloadAndReadData(
                services.BomHttpClient,
                context,
                startDate,
                endDate,
                logger),
            RecentObservationStationSource.Ghcnd => await RecentObservationsGhcndDataSource.DownloadAndReadData(
                services.GhcndHttpClient,
                context,
                startDate,
                endDate,
                logger),
            _ => null,
        };
    }

    private static RecentObservationSeries CreateSeries(
        RecentObservationStationSource source,
        RecentObservationSeriesDownload download,
        DateTimeOffset retrievedAtUtc)
    {
        if (download == null)
        {
            return null;
        }

        return new RecentObservationSeries
        {
            Records = download.Records,
            DataAdjustment = download.MeasurementDefinition.DataAdjustment,
            DataResolution = download.MeasurementDefinition.DataResolution,
            UnitOfMeasure = download.MeasurementDefinition.UnitOfMeasure,
            SourceMetadata = new RecentObservationSourceMetadata
            {
                SourceCode = GetSourceCode(source),
                SourceName = GetSourceName(source),
                StationId = download.StationId,
                SourceUrl = download.SourceUrl,
                SourceUrlLabel = download.SourceUrlLabel,
                RetrievedAtUtc = retrievedAtUtc.ToUniversalTime(),
            },
        };
    }

    private static string GetSourceCode(RecentObservationStationSource source)
    {
        return source switch
        {
            RecentObservationStationSource.Bom => "BOM",
            RecentObservationStationSource.Ghcnd => "GHCNd",
            _ => source.ToString(),
        };
    }

    private static string GetSourceName(RecentObservationStationSource source)
    {
        return source switch
        {
            RecentObservationStationSource.Bom => "Australian Bureau of Meteorology",
            RecentObservationStationSource.Ghcnd => "Global Historical Climatology Network Daily",
            _ => source.ToString(),
        };
    }

    private static bool HasRecordForDate(RecentObservationsResponse response, DateOnly date)
    {
        return AllSeries(response).Any(series =>
            series.Records.Any(x => x.Year == date.Year && x.Month == date.Month && x.Day == date.Day));
    }

    private static bool HasSourceMetadata(RecentObservationsResponse response)
    {
        return AllSeries(response).Any(series => series.SourceMetadata is
        {
            SourceCode: not null,
            SourceName: not null,
            StationId: not null,
            SourceUrl: not null,
            SourceUrlLabel: not null,
            RetrievedAtUtc: not null,
        });
    }

    private static IEnumerable<RecentObservationSeries> AllSeries(RecentObservationsResponse response)
    {
        if (response == null)
        {
            yield break;
        }

        if (response.TempMax != null)
        {
            yield return response.TempMax;
        }

        if (response.TempMin != null)
        {
            yield return response.TempMin;
        }

        if (response.Precipitation != null)
        {
            yield return response.Precipitation;
        }
    }

    private static bool WasDataRetrievedInLastHours(RecentObservationsResponse response, int hours = 24)
    {
        return response?.RetrievedDate >= DateTimeOffset.UtcNow.AddHours(-1 * hours);
    }
}
