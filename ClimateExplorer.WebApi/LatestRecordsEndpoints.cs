namespace ClimateExplorer.WebApi;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Bom;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;

internal static class LatestRecordsEndpoints
{
    public static async Task<LatestRecordsResponse> GetLatestRecords(
        [FromServices] ClimateExplorerApiServices services,
        [FromServices] ILoggerFactory loggerFactory,
        Guid locationId,
        DataType dataType = DataType.TempMax,
        bool isLocationSupported = false)
    {
        var logger = loggerFactory.CreateLogger(nameof(LatestRecordsEndpoints));
        var today = DateOnly.FromDateTime(DateTime.Today);
        var bomContext = await GetBomLatestRecordsContext(services, locationId, dataType, today);
        if (isLocationSupported || !bomContext.IsSupported)
        {
            return new LatestRecordsResponse { IsSupported = bomContext.IsSupported };
        }

        var cacheKey = $"{ClimateExplorerApiConstants.LatestRecordsCacheKeyPrefix}_{locationId}_{dataType}";
        var cachedResponse = await services.Cache.Get<LatestRecordsResponse>(cacheKey);
        if (HasRecordForDate(cachedResponse, today) || WasDataRetrievedInLastHour(cachedResponse))
        {
            return cachedResponse;
        }

        try
        {
            var outputDirectory = Path.Combine(ClimateExplorerApiConstants.LatestRecordsDataFolder, bomContext.MeasurementDefinition.FolderName);
            var downloadedFile = await BomDataDownloader.DownloadAndExtractDailyBomData(
                services.BomHttpClient,
                locationId,
                dataType,
                bomContext.DataFileMapping,
                outputDirectory,
                overwriteExistingZip: true);

            if (downloadedFile == null || !File.Exists(downloadedFile))
            {
                return cachedResponse ?? new LatestRecordsResponse { IsSupported = true };
            }

            var records = await ReadLatestBomClimateRecords(
                bomContext.StationId,
                bomContext.MeasurementDefinition,
                outputDirectory,
                today.Year);

            var response = new LatestRecordsResponse
            {
                RetrievedDate = DateTimeOffset.UtcNow,
                IsSupported = true,
                DataAdjustment = bomContext.MeasurementDefinition.DataAdjustment,
                DataResolution = bomContext.MeasurementDefinition.DataResolution,
                DataType = bomContext.MeasurementDefinition.DataType,
                UnitOfMeasure = bomContext.MeasurementDefinition.UnitOfMeasure,
                Records = records,
            };

            await services.Cache.Put(cacheKey, response);
            DeleteLatestBomDownloadFiles(downloadedFile, logger);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to retrieve latest BOM records for location {LocationId} and data type {DataType}", locationId, dataType);
            return cachedResponse ?? new LatestRecordsResponse { IsSupported = true };
        }
    }

    private static async Task<(bool IsSupported, string StationId, MeasurementDefinition MeasurementDefinition, DataFileMapping DataFileMapping)> GetBomLatestRecordsContext(
        ClimateExplorerApiServices services,
        Guid locationId,
        DataType dataType,
        DateOnly asAt)
    {
        if (!dataType.TryToObsCode(out _))
        {
            return (false, null, null, null);
        }

        var locations = await LocationEndpoints.GetCachedLocations(services, locationId, permitCreateCache: false);
        var location = locations.SingleOrDefault(x => x.Id == locationId);
        if (location == null)
        {
            return (false, null, null, null);
        }

        var dataSetDefinitions = await DataSetDefinition.GetDataSetDefinitions();
        var bomDataSetDefinition = dataSetDefinitions.SingleOrDefault(x => x.Id == ClimateExplorerApiConstants.BomDataSetDefinitionId);
        var dataFileMapping = bomDataSetDefinition?.DataLocationMapping;
        if (dataFileMapping == null || !dataFileMapping.LocationIdToDataFileMappings.ContainsKey(location.Id))
        {
            return (false, null, null, null);
        }

        var measurementDefinition = bomDataSetDefinition!.MeasurementDefinitions?.SingleOrDefault(x =>
            x.DataType == dataType &&
            x.DataResolution == DataResolution.Daily &&
            x.RowDataType == RowDataType.OneValuePerRow);
        if (measurementDefinition == null)
        {
            return (false, null, null, null);
        }

        var stationId = BomDataDownloader.GetMostRecentOperatingStationId(location.Id, dataFileMapping, asAt);
        if (stationId == null)
        {
            return (false, null, null, null);
        }

        return (true, stationId, measurementDefinition, dataFileMapping);
    }

    private static async Task<List<DataRecord>> ReadLatestBomClimateRecords(
        string stationId,
        MeasurementDefinition measurementDefinition,
        string outputDirectory,
        int year)
    {
        var latestMeasurementDefinition = new MeasurementDefinition
        {
            DataAdjustment = measurementDefinition.DataAdjustment,
            DataResolution = measurementDefinition.DataResolution,
            DataRowRegEx = measurementDefinition.DataRowRegEx,
            DataType = measurementDefinition.DataType,
            FileNameFormat = measurementDefinition.FileNameFormat,
            FolderName = outputDirectory,
            NullValue = measurementDefinition.NullValue,
            RowDataType = measurementDefinition.RowDataType,
            UnitOfMeasure = measurementDefinition.UnitOfMeasure,
            ValueAdjustment = measurementDefinition.ValueAdjustment,
        };

        var dataRecords = await DataReaderFunctions.GetDataRecords(
            latestMeasurementDefinition,
            [
                new DataFileFilterAndAdjustment
                {
                    Id = stationId,
                    StartDate = new DateOnly(year, 1, 1),
                },
            ]);

        return dataRecords
            .Where(x => x.Value.HasValue && x.Month.HasValue && x.Day.HasValue)
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Day)
            .Select(x => new DataRecord(x.Year, x.Month, x.Day, x.Value))
            .ToList();
    }

    private static void DeleteLatestBomDownloadFiles(string downloadedFile, ILogger logger)
    {
        DeleteFileIfExists(downloadedFile, logger);

        var zipFilePath = Path.Combine("Output", "Temp", $"{Path.GetFileNameWithoutExtension(downloadedFile)}.zip");
        DeleteFileIfExists(zipFilePath, logger);

        DeleteDirectoryIfEmpty(Path.GetDirectoryName(downloadedFile), logger);
    }

    private static void DeleteFileIfExists(string filePath, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to delete latest BOM temporary file {FilePath}", filePath);
        }
    }

    private static void DeleteDirectoryIfEmpty(string directoryPath, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            if (!Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                Directory.Delete(directoryPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to delete latest BOM temporary directory {DirectoryPath}", directoryPath);
        }
    }

    private static bool HasRecordForDate(LatestRecordsResponse response, DateOnly date)
    {
        return response?.Records.Any(x => x.Year == date.Year && x.Month == date.Month && x.Day == date.Day) == true;
    }

    private static bool WasDataRetrievedInLastHour(LatestRecordsResponse response)
    {
        return response?.RetrievedDate >= DateTimeOffset.UtcNow.AddHours(-1);
    }
}
