namespace ClimateExplorer.WebApi.RecentObservations;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;

internal static partial class RecentObservationsBomDataSource
{
    private enum DailyBomObsCode
    {
        DailyTempMax = 122,
        DailyTempMin = 123,
        DailyRainfall = 136,
        DailySolarRadiation = 193,
    }

    public static RecentObservationsContext GetContext(
        Location location,
        DataType dataType,
        DateOnly asAt,
        IEnumerable<DataSetDefinition> dataSetDefinitions)
    {
        if (!TryGetDailyBomObsCode(dataType, out _))
        {
            return null;
        }

        return RecentObservationsDataSourceHelpers.GetContext(
            location,
            dataType,
            asAt,
            dataSetDefinitions,
            ClimateExplorerApiConstants.BomDataSetDefinitionId,
            RecentObservationStationSource.Bom);
    }

    public static async Task<List<DataRecord>> DownloadAndReadData(
        HttpClient httpClient,
        DataType dataType,
        RecentObservationsContext context,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        if (!TryGetDailyBomObsCode(dataType, out var obsCode))
        {
            return null;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ClimateExplorerRecentObservations_{Guid.NewGuid():N}");
        var zipFilePath = Path.Combine(tempDirectory, $"{context.StationId}_{obsCode.ToString().ToLowerInvariant()}.zip");

        try
        {
            var pC = await GetDailyBomPC(httpClient, context.StationId, obsCode, logger);
            if (pC == null)
            {
                return null;
            }

            Directory.CreateDirectory(tempDirectory);

            var zipFileUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/weatherData/av?p_display_type=dailyZippedDataFile&p_stn_num={context.StationId}&p_nccObsCode={(int)obsCode}&p_c={pC}";
            using var zipFileResponse = await httpClient.GetAsync(zipFileUrl);
            if (!zipFileResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Unable to download recent BOM zip for station {StationId} and ObsCode {ObsCode}. Status code: {StatusCode}", context.StationId, obsCode, zipFileResponse.StatusCode);
                return null;
            }

            await using (var fs = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await zipFileResponse.Content.CopyToAsync(fs);
            }

            var lines = await ReadCsvLinesFromZipFile(zipFilePath, logger);
            if (lines == null)
            {
                return null;
            }

            return ReadRecords(context.StationId, context.MeasurementDefinition, lines, startDate, endDate);
        }
        finally
        {
            RecentObservationsDataSourceHelpers.DeleteFileIfExists(zipFilePath, logger);
            RecentObservationsDataSourceHelpers.DeleteDirectoryIfEmpty(tempDirectory, logger);
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

    private static async Task<string[]> ReadCsvLinesFromZipFile(string zipFilePath, ILogger logger)
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

    private static List<DataRecord> ReadRecords(
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

        var dataRecords = fileRecords.Values.Select(x => x.Value.HasValue
            ? x.WithValue(RecentObservationsDataSourceHelpers.ApplyValueAdjustment(x.Value.Value, measurementDefinition))
            : x);

        return RecentObservationsDataSourceHelpers.OrderDailyRecords(dataRecords);
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

    [GeneratedRegex(@"\d{6}\|\|,(?<startYear>\d{4}):(?<p_c>-?\d+),")]
    private static partial Regex DailyBomAvailableYearsRegex();
}
