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
        DataType dataType,
        DateOnly asAt,
        IEnumerable<DataSetDefinition> dataSetDefinitions)
    {
        if (!TryGetDataSetDefinitionId(dataType, out var dataSetDefinitionId))
        {
            return null;
        }

        return RecentObservationsDataSourceHelpers.GetContext(
            location,
            dataType,
            asAt,
            dataSetDefinitions,
            dataSetDefinitionId,
            RecentObservationStationSource.Ghcnd);
    }

    public static async Task<List<DataRecord>> DownloadAndReadData(
        HttpClient httpClient,
        DataType dataType,
        RecentObservationsContext context,
        DateOnly startDate,
        DateOnly endDate,
        ILogger logger)
    {
        if (!TryGetDataSetDefinitionId(dataType, out _))
        {
            return null;
        }

        var csvContent = await GhcndStationCsvDownloader.DownloadCsvAsync(httpClient, context.StationId);
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            logger.LogWarning("Unable to download recent GHCNd CSV for station {StationId}. Response was empty.", context.StationId);
            return null;
        }

        var rows = GhcndCsvReader.RemoveRowsWithNoData(GhcndCsvReader.ReadRows(csvContent));
        return dataType switch
        {
            DataType.TempMax => ReadRecords(
                context.StationId,
                context.MeasurementDefinition,
                rows,
                GhcndTemperatureProcessor.CreateRecords,
                GhcndTemperatureProcessor.ValidateRecords,
                record => record.Date,
                record => record.Tmax,
                startDate,
                endDate,
                logger),
            DataType.TempMin => ReadRecords(
                context.StationId,
                context.MeasurementDefinition,
                rows,
                GhcndTemperatureProcessor.CreateRecords,
                GhcndTemperatureProcessor.ValidateRecords,
                record => record.Date,
                record => record.Tmin,
                startDate,
                endDate,
                logger),
            DataType.Precipitation => ReadRecords(
                context.StationId,
                context.MeasurementDefinition,
                rows,
                GhcndPrecipitationProcessor.CreateRecords,
                GhcndPrecipitationProcessor.ValidateRecords,
                record => record.Date,
                record => record.Precipitation,
                startDate,
                endDate,
                logger),
            _ => null,
        };
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

    private static bool TryGetDataSetDefinitionId(DataType dataType, out Guid dataSetDefinitionId)
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
}
