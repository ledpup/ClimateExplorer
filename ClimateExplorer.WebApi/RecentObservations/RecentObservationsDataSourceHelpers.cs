namespace ClimateExplorer.WebApi.RecentObservations;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClimateExplorer.Core.Model;
using Microsoft.Extensions.Logging;
using static ClimateExplorer.Core.Enums;

internal static class RecentObservationsDataSourceHelpers
{
    /// <summary>
    /// Resolves the most recent operating station id for a location within a data
    /// set definition, or <c>null</c> when the location is not mapped to that data
    /// set or no station is operating as at <paramref name="asAt"/>.
    /// </summary>
    public static string GetStationId(
        DataSetDefinition dataSetDefinition,
        Location location,
        DateOnly asAt)
    {
        var dataFileMapping = dataSetDefinition?.DataLocationMapping;
        if (dataFileMapping == null || !dataFileMapping.LocationIdToDataFileMappings.ContainsKey(location.Id))
        {
            return null;
        }

        return GetMostRecentOperatingStationId(location.Id, dataFileMapping, asAt);
    }

    /// <summary>
    /// Builds a series context for a data type from a resolved station, or
    /// <c>null</c> when the data set has no matching daily measurement definition.
    /// </summary>
    public static RecentObservationSeriesContext CreateSeriesContext(
        DataSetDefinition dataSetDefinition,
        DataType dataType,
        string stationId)
    {
        if (stationId == null)
        {
            return null;
        }

        var measurementDefinition = dataSetDefinition?.MeasurementDefinitions?.SingleOrDefault(x =>
            x.DataType == dataType &&
            x.DataResolution == DataResolution.Daily &&
            x.RowDataType == RowDataType.OneValuePerRow);

        return measurementDefinition == null
            ? null
            : new RecentObservationSeriesContext(stationId, measurementDefinition);
    }

    public static bool TryCreateAdjustedDailyRecord(
        string dateValue,
        int? rawValue,
        int? nullValue,
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
            || (nullValue.HasValue && rawValue.Value == nullValue.Value))
        {
            return false;
        }

        dataRecord = new DataRecord(date, ApplyValueAdjustment(rawValue.Value, measurementDefinition));
        return true;
    }

    public static double ApplyValueAdjustment(double value, MeasurementDefinition measurementDefinition)
    {
        return measurementDefinition.ValueAdjustment.HasValue
            ? value / measurementDefinition.ValueAdjustment.Value
            : value;
    }

    public static List<DataRecord> OrderDailyRecords(IEnumerable<DataRecord> dataRecords)
    {
        return dataRecords
            .Where(x => x.Value.HasValue && x.Month.HasValue && x.Day.HasValue)
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Day)
            .ToList();
    }

    public static void DeleteFileIfExists(string filePath, ILogger logger)
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

    public static void DeleteDirectoryIfEmpty(string directoryPath, ILogger logger)
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
}
