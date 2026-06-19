namespace ClimateExplorer.WebApi.RecentObservations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Ghcnd;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;

internal static class RecentObservationsGhcndDataSource
{
    public static RecentObservationsContext GetContext(
        Location location,
        DateOnly asAt,
        IEnumerable<DataSetDefinition> dataSetDefinitions)
    {
        var temperatureDsd = dataSetDefinitions.SingleOrDefault(x => x.Id == ClimateExplorerApiConstants.GhcndTemperatureDataSetDefinitionId);
        var precipitationDsd = dataSetDefinitions.SingleOrDefault(x => x.Id == ClimateExplorerApiConstants.GhcndPrecipitationDataSetDefinitionId);

        var temperatureStationId = temperatureDsd == null ? null : RecentObservationsDataSourceHelpers.GetStationId(temperatureDsd, location, asAt);
        var precipitationStationId = precipitationDsd == null ? null : RecentObservationsDataSourceHelpers.GetStationId(precipitationDsd, location, asAt);

        var context = new RecentObservationsContext(
            RecentObservationStationSource.Ghcnd,
            RecentObservationsDataSourceHelpers.CreateSeriesContext(temperatureDsd, DataType.TempMax, temperatureStationId),
            RecentObservationsDataSourceHelpers.CreateSeriesContext(temperatureDsd, DataType.TempMin, temperatureStationId),
            RecentObservationsDataSourceHelpers.CreateSeriesContext(precipitationDsd, DataType.Precipitation, precipitationStationId));

        return context.HasAnySeries ? context : null;
    }

    public static async Task<RecentObservationsDownloadResult> DownloadAndReadData(
        HttpClient httpClient,
        RecentObservationsContext context,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        // Cache parsed rows by station so a station file shared across series (the
        // common case where max, min and precipitation come from one station) is
        // downloaded and parsed only once.
        var parsedRowsByStation = new Dictionary<string, List<GhcndInputRow>>(StringComparer.OrdinalIgnoreCase);

        async Task<List<GhcndInputRow>> GetRows(string stationId)
        {
            if (parsedRowsByStation.TryGetValue(stationId, out var cached))
            {
                return cached;
            }

            var csvContent = await GhcndStationCsvDownloader.DownloadCsvAsync(httpClient, stationId);
            if (string.IsNullOrWhiteSpace(csvContent))
            {
                logger.LogWarning("Unable to download recent GHCNd CSV for station {StationId}. Response was empty.", stationId);
                parsedRowsByStation[stationId] = null;
                return null;
            }

            var rows = GhcndCsvReader.RemoveRowsWithNoData(GhcndCsvReader.ReadRows(csvContent));
            parsedRowsByStation[stationId] = rows;
            return rows;
        }

        var tempMax = await ReadTemperatureSeries(context.TempMax, GetRows, record => record.Tmax, startDate, endDate, logger);
        var tempMin = await ReadTemperatureSeries(context.TempMin, GetRows, record => record.Tmin, startDate, endDate, logger);
        var precipitation = await ReadPrecipitationSeries(context.Precipitation, GetRows, startDate, endDate, logger);

        var result = new RecentObservationsDownloadResult(tempMax, tempMin, precipitation);
        return result.HasAnySeries ? result : null;
    }

    private static async Task<RecentObservationSeriesDownload> ReadTemperatureSeries(
        RecentObservationSeriesContext seriesContext,
        Func<string, Task<List<GhcndInputRow>>> getRows,
        Func<OutputRowTemperature, int?> getValue,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        if (seriesContext == null)
        {
            return null;
        }

        var rows = await getRows(seriesContext.StationId);
        if (rows == null)
        {
            return null;
        }

        var records = ReadRecords(
            seriesContext.StationId,
            seriesContext.MeasurementDefinition,
            rows,
            GhcndTemperatureProcessor.CreateRecords,
            GhcndTemperatureProcessor.ValidateRecords,
            record => record.Date,
            getValue,
            startDate,
            endDate,
            logger);

        return CreateDownload(seriesContext, records);
    }

    private static async Task<RecentObservationSeriesDownload> ReadPrecipitationSeries(
        RecentObservationSeriesContext seriesContext,
        Func<string, Task<List<GhcndInputRow>>> getRows,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        if (seriesContext == null)
        {
            return null;
        }

        var rows = await getRows(seriesContext.StationId);
        if (rows == null)
        {
            return null;
        }

        var records = ReadRecords(
            seriesContext.StationId,
            seriesContext.MeasurementDefinition,
            rows,
            GhcndPrecipitationProcessor.CreateRecords,
            GhcndPrecipitationProcessor.ValidateRecords,
            record => record.Date,
            record => record.Precipitation,
            startDate,
            endDate,
            logger);

        return CreateDownload(seriesContext, records);
    }

    private static RecentObservationSeriesDownload CreateDownload(
        RecentObservationSeriesContext seriesContext,
        List<DataRecord> records)
    {
        return new RecentObservationSeriesDownload(
            records,
            seriesContext.MeasurementDefinition,
            seriesContext.StationId,
            GhcndStationCsvDownloader.GetDownloadUrl(seriesContext.StationId),
            CreateSourceUrlLabel(seriesContext.StationId));
    }

    private static List<DataRecord> ReadRecords<TRecord>(
        string stationId,
        MeasurementDefinition measurementDefinition,
        IEnumerable<GhcndInputRow> rows,
        Func<IEnumerable<GhcndInputRow>, List<TRecord>> createRecords,
        Action<List<TRecord>, string, ILogger> validateRecords,
        Func<TRecord, string> getDate,
        Func<TRecord, int?> getValue,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        var records = createRecords(rows);
        validateRecords(records, stationId, logger);

        var dataRecords = new List<DataRecord>();
        foreach (var record in records)
        {
            if (RecentObservationsDataSourceHelpers.TryCreateAdjustedDailyRecord(
                getDate(record),
                getValue(record),
                GhcndConstants.NullRecord,
                measurementDefinition,
                startDate,
                endDate,
                out var dataRecord))
            {
                dataRecords.Add(dataRecord);
            }
        }

        return RecentObservationsDataSourceHelpers.OrderDailyRecords(dataRecords);
    }

    private static string CreateSourceUrlLabel(string stationId)
    {
        return $"Station {stationId}, CSV";
    }
}
