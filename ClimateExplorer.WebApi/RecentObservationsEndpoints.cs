namespace ClimateExplorer.WebApi;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Ghcnd;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;

internal static partial class RecentObservationsEndpoints
{
    private enum DailyBomObsCode
    {
        DailyTempMax = 122,
        DailyTempMin = 123,
        DailyRainfall = 136,
        DailySolarRadiation = 193,
    }

    private enum RecentObservationStationSource
    {
        Bom,
        Ghcnd,
    }

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
        if (HasRecordForDate(cachedResponse, today) || HasRecordForDate(cachedResponse, yesterday) || WasDataRetrievedInLastHours(cachedResponse, 12))
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
        return GetBomRecentObservationsContext(location, dataType, asAt, dataSetDefinitions)
            ?? GetGhcndRecentObservationsContext(location, dataType, asAt, dataSetDefinitions);
    }

    private static RecentObservationsContext GetBomRecentObservationsContext(
        Location location,
        DataType dataType,
        DateOnly asAt,
        List<DataSetDefinition> dataSetDefinitions)
    {
        if (!TryGetDailyBomObsCode(dataType, out _))
        {
            return null;
        }

        var bomDataSetDefinition = dataSetDefinitions.SingleOrDefault(x => x.Id == ClimateExplorerApiConstants.BomDataSetDefinitionId);
        var dataFileMapping = bomDataSetDefinition?.DataLocationMapping;
        if (dataFileMapping == null || !dataFileMapping.LocationIdToDataFileMappings.ContainsKey(location.Id))
        {
            return null;
        }

        var measurementDefinition = bomDataSetDefinition!.MeasurementDefinitions?.SingleOrDefault(x =>
            x.DataType == dataType &&
            x.DataResolution == DataResolution.Daily &&
            x.RowDataType == RowDataType.OneValuePerRow);
        if (measurementDefinition == null)
        {
            return null;
        }

        var stationId = GetMostRecentOperatingStationId(location.Id, dataFileMapping, asAt);
        if (stationId == null)
        {
            return null;
        }

        return new RecentObservationsContext(RecentObservationStationSource.Bom, stationId, measurementDefinition);
    }

    private static RecentObservationsContext GetGhcndRecentObservationsContext(
        Location location,
        DataType dataType,
        DateOnly asAt,
        List<DataSetDefinition> dataSetDefinitions)
    {
        if (!TryGetGhcndDataSetDefinitionId(dataType, out var dataSetDefinitionId))
        {
            return null;
        }

        var dataSetDefinition = dataSetDefinitions.SingleOrDefault(x => x.Id == dataSetDefinitionId);
        var dataFileMapping = dataSetDefinition?.DataLocationMapping;
        if (dataFileMapping == null || !dataFileMapping.LocationIdToDataFileMappings.ContainsKey(location.Id))
        {
            return null;
        }

        var measurementDefinition = dataSetDefinition!.MeasurementDefinitions?.SingleOrDefault(x =>
            x.DataType == dataType &&
            x.DataResolution == DataResolution.Daily &&
            x.RowDataType == RowDataType.OneValuePerRow);
        if (measurementDefinition == null)
        {
            return null;
        }

        var stationId = GetMostRecentOperatingStationId(location.Id, dataFileMapping, asAt);
        if (stationId == null)
        {
            return null;
        }

        return new RecentObservationsContext(RecentObservationStationSource.Ghcnd, stationId, measurementDefinition);
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
            RecentObservationStationSource.Bom => await DownloadAndReadDailyBomData(
                services.BomHttpClient,
                dataType,
                context.StationId,
                context.MeasurementDefinition,
                startDate,
                endDate,
                logger),
            RecentObservationStationSource.Ghcnd => await DownloadAndReadDailyGhcndData(
                services.GhcndHttpClient,
                dataType,
                context.StationId,
                context.MeasurementDefinition,
                startDate,
                endDate,
                logger),
            _ => null,
        };
    }

    private static async Task<List<DataRecord>> DownloadAndReadDailyBomData(
        HttpClient httpClient,
        DataType dataType,
        string stationId,
        MeasurementDefinition measurementDefinition,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        if (!TryGetDailyBomObsCode(dataType, out var obsCode))
        {
            return null;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ClimateExplorerRecentObservations_{Guid.NewGuid():N}");
        var zipFilePath = Path.Combine(tempDirectory, $"{stationId}_{obsCode.ToString().ToLowerInvariant()}.zip");

        try
        {
            var pC = await GetDailyBomPC(httpClient, stationId, obsCode, logger);
            if (pC == null)
            {
                return null;
            }

            Directory.CreateDirectory(tempDirectory);

            var zipFileUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/weatherData/av?p_display_type=dailyZippedDataFile&p_stn_num={stationId}&p_nccObsCode={(int)obsCode}&p_c={pC}";
            using var zipFileResponse = await httpClient.GetAsync(zipFileUrl);
            if (!zipFileResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Unable to download recent BOM zip for station {StationId} and ObsCode {ObsCode}. Status code: {StatusCode}", stationId, obsCode, zipFileResponse.StatusCode);
                return null;
            }

            await using (var fs = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await zipFileResponse.Content.CopyToAsync(fs);
            }

            var lines = await ReadBomCsvLinesFromZipFile(zipFilePath, logger);
            if (lines == null)
            {
                return null;
            }

            return ReadRecentObservationsBom(stationId, measurementDefinition, lines, startDate, endDate);
        }
        finally
        {
            DeleteFileIfExists(zipFilePath, logger);
            DeleteDirectoryIfEmpty(tempDirectory, logger);
        }
    }

    private static async Task<string> GetDailyBomPC(
        HttpClient httpClient,
        string stationId,
        DailyBomObsCode obsCode,
        ILogger logger)
    {
        var availableYearsUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/wData/wdata?p_stn_num={stationId}&p_display_type=availableYears&p_nccObsCode={(int)obsCode}";
        using var response = await httpClient.GetAsync(availableYearsUrl);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Unable to retrieve recent BOM available years for station {StationId} and ObsCode {ObsCode}. Status code: {StatusCode}", stationId, obsCode, response.StatusCode);
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var match = DailyBomAvailableYearsRegex().Match(responseContent);
        if (!match.Success)
        {
            logger.LogWarning("Unable to find recent BOM p_c value for station {StationId} and ObsCode {ObsCode}", stationId, obsCode);
            return null;
        }

        return match.Groups["p_c"].Value;
    }

    private static async Task<string[]> ReadBomCsvLinesFromZipFile(string zipFilePath, ILogger logger)
    {
        try
        {
            using var zipFileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Read);
            var csvEntries = archive.Entries
                .Where(x => x.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (csvEntries.Count != 1)
            {
                logger.LogWarning("Expected one CSV entry in recent BOM zip {ZipFilePath}, found {CsvEntryCount}", zipFilePath, csvEntries.Count);
                return null;
            }

            using var sr = new StreamReader(csvEntries.Single().Open());
            var lines = new List<string>();
            while (await sr.ReadLineAsync() is { } line)
            {
                lines.Add(line);
            }

            return lines.ToArray();
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "Unable to read recent BOM zip file {ZipFilePath}. File may be corrupt.", zipFilePath);
            return null;
        }
    }

    private static List<DataRecord> ReadRecentObservationsBom(
        string stationId,
        MeasurementDefinition measurementDefinition,
        string[] lines,
        DateOnly startDate,
        DateOnly endDate)
    {
        var regEx = new Regex(measurementDefinition.DataRowRegEx!);
        var fileRecords = DataReaderFunctions.ProcessDataFile(
            lines,
            regEx,
            measurementDefinition.NullValue!,
            measurementDefinition.DataResolution,
            stationId,
            startDate,
            endDate);

        var dataRecords = fileRecords.Values.ToList();

        if (measurementDefinition.ValueAdjustment != null)
        {
            dataRecords.ForEach(x => x.Value = x.Value / measurementDefinition.ValueAdjustment.Value);
        }

        return dataRecords
            .Where(x => x.Value.HasValue && x.Month.HasValue && x.Day.HasValue)
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Day)
            .Select(x => new DataRecord(x.Year, x.Month, x.Day, x.Value))
            .ToList();
    }

    private static async Task<List<DataRecord>> DownloadAndReadDailyGhcndData(
        HttpClient httpClient,
        DataType dataType,
        string stationId,
        MeasurementDefinition measurementDefinition,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        if (!TryGetGhcndDataSetDefinitionId(dataType, out _))
        {
            return null;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ClimateExplorerRecentObservations_{Guid.NewGuid():N}");
        var csvFilePath = Path.Combine(tempDirectory, $"{stationId}.csv");

        try
        {
            var csvContent = await GhcndStationCsvDownloader.DownloadCsvAsync(httpClient, stationId);
            if (string.IsNullOrWhiteSpace(csvContent))
            {
                logger.LogWarning("Unable to download recent GHCNd CSV for station {StationId}. Response was empty.", stationId);
                return null;
            }

            Directory.CreateDirectory(tempDirectory);
            await File.WriteAllTextAsync(csvFilePath, csvContent);

            using var reader = new StreamReader(csvFilePath);
            var rows = GhcndCsvReader.ReadRows(reader);
            var rowsWithData = GhcndCsvReader.RemoveRowsWithNoData(rows);

            return dataType switch
            {
                DataType.TempMax or DataType.TempMin => ReadRecentObservationsGhcndTemperature(
                    stationId,
                    dataType,
                    measurementDefinition,
                    rowsWithData,
                    startDate,
                    endDate,
                    logger),
                DataType.Precipitation => ReadRecentObservationsGhcndPrecipitation(
                    stationId,
                    measurementDefinition,
                    rowsWithData,
                    startDate,
                    endDate,
                    logger),
                _ => null,
            };
        }
        finally
        {
            DeleteFileIfExists(csvFilePath, logger);
            DeleteDirectoryIfEmpty(tempDirectory, logger);
        }
    }

    private static List<DataRecord> ReadRecentObservationsGhcndTemperature(
        string stationId,
        DataType dataType,
        MeasurementDefinition measurementDefinition,
        List<GhcndInputRow> rows,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        var records = GhcndTemperatureProcessor.CreateRecords(rows);
        GhcndTemperatureProcessor.ValidateRecords(records, stationId, logger);

        var dataRecords = new List<DataRecord>();
        foreach (var record in records)
        {
            var rawValue = dataType switch
            {
                DataType.TempMax => record.Tmax,
                DataType.TempMin => record.Tmin,
                _ => null,
            };

            if (TryCreateGhcndDataRecord(record.Date, rawValue, measurementDefinition, startDate, endDate, out var dataRecord))
            {
                dataRecords.Add(dataRecord);
            }
        }

        return OrderDailyRecords(dataRecords);
    }

    private static List<DataRecord> ReadRecentObservationsGhcndPrecipitation(
        string stationId,
        MeasurementDefinition measurementDefinition,
        List<GhcndInputRow> rows,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        var records = GhcndPrecipitationProcessor.CreateRecords(rows);
        GhcndPrecipitationProcessor.ValidateRecords(records, stationId, logger);

        var dataRecords = new List<DataRecord>();
        foreach (var record in records)
        {
            if (TryCreateGhcndDataRecord(record.Date, record.Precipitation, measurementDefinition, startDate, endDate, out var dataRecord))
            {
                dataRecords.Add(dataRecord);
            }
        }

        return OrderDailyRecords(dataRecords);
    }

    private static bool TryCreateGhcndDataRecord(
        string dateValue,
        int? rawValue,
        MeasurementDefinition measurementDefinition,
        DateOnly startDate,
        DateOnly endDate,
        out DataRecord dataRecord)
    {
        dataRecord = null;

        if (!DateOnly.TryParseExact(dateValue, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            || date < startDate
            || date > endDate
            || !rawValue.HasValue
            || rawValue.Value == GhcndConstants.NullRecord)
        {
            return false;
        }

        var value = (double)rawValue.Value;
        if (measurementDefinition.ValueAdjustment.HasValue)
        {
            value /= measurementDefinition.ValueAdjustment.Value;
        }

        dataRecord = new DataRecord(date, value);
        return true;
    }

    private static List<DataRecord> OrderDailyRecords(IEnumerable<DataRecord> dataRecords)
    {
        return dataRecords
            .Where(x => x.Value.HasValue && x.Month.HasValue && x.Day.HasValue)
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Day)
            .ToList();
    }

    private static string GetMostRecentOperatingStationId(Guid locationId, DataFileMapping dataFileMapping, DateOnly asAt)
    {
        if (!dataFileMapping.LocationIdToDataFileMappings.TryGetValue(locationId, out var dataFileFilterAndAdjustments))
        {
            return null;
        }

        return dataFileFilterAndAdjustments
            .Where(x => (!x.StartDate.HasValue || x.StartDate.Value <= asAt) && (!x.EndDate.HasValue || x.EndDate.Value >= asAt))
            .OrderByDescending(x => x.StartDate ?? DateOnly.MinValue)
            .FirstOrDefault()
            ?.Id;
    }

    private static bool TryGetDailyBomObsCode(DataType dataType, out DailyBomObsCode obsCode)
    {
        switch (dataType)
        {
            case DataType.TempMax:
                obsCode = DailyBomObsCode.DailyTempMax;
                return true;

            case DataType.TempMin:
                obsCode = DailyBomObsCode.DailyTempMin;
                return true;

            case DataType.Precipitation:
                obsCode = DailyBomObsCode.DailyRainfall;
                return true;

            case DataType.SolarRadiation:
                obsCode = DailyBomObsCode.DailySolarRadiation;
                return true;

            default:
                obsCode = default;
                return false;
        }
    }

    private static bool TryGetGhcndDataSetDefinitionId(DataType dataType, out Guid dataSetDefinitionId)
    {
        switch (dataType)
        {
            case DataType.TempMax:
            case DataType.TempMin:
                dataSetDefinitionId = ClimateExplorerApiConstants.GhcndTemperatureDataSetDefinitionId;
                return true;

            case DataType.Precipitation:
                dataSetDefinitionId = ClimateExplorerApiConstants.GhcndPrecipitationDataSetDefinitionId;
                return true;

            default:
                dataSetDefinitionId = default;
                return false;
        }
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
            logger.LogWarning(ex, "Unable to delete recent observations temporary file {FilePath}", filePath);
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
            logger.LogWarning(ex, "Unable to delete recent observations temporary directory {DirectoryPath}", directoryPath);
        }
    }

    private static bool HasRecordForDate(RecentObservationsResponse response, DateOnly date)
    {
        return response?.Records.Any(x => x.Year == date.Year && x.Month == date.Month && x.Day == date.Day) == true;
    }

    private static bool WasDataRetrievedInLastHours(RecentObservationsResponse response, int hours = 24)
    {
        return response?.RetrievedDate >= DateTimeOffset.UtcNow.AddHours(-1 * hours);
    }

    [GeneratedRegex(@"\d{6}\|\|,(?<startYear>\d{4}):(?<p_c>-?\d+),")]
    private static partial Regex DailyBomAvailableYearsRegex();

    private sealed record RecentObservationsContext(
        RecentObservationStationSource Source,
        string StationId,
        MeasurementDefinition MeasurementDefinition);
}
