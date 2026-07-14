namespace ClimateExplorer.Data.Downloading.Orchestration;

using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Models;
using static ClimateExplorer.Core.Enums;

public sealed class DataSetDownloadValidator
{
    /// <summary>
    /// Validates every measurement in the request and returns the latest daily record date seen,
    /// so freshness checks for daily downloaders (see <see cref="DataSetFreshnessPolicy"/>) can tell
    /// whether the source already has yesterday's/today's row without a separate parse. The result is
    /// the minimum across the request's measurements (the worst-covered one), not the maximum, so a
    /// lagging measurement bundled into the same archive as an up-to-date one isn't masked. Returns
    /// null if none of the measurements are <see cref="DataResolution.Daily"/>.
    /// </summary>
    public async Task<DateOnly?> ValidateAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
        DateOnly? latestRecordDate = null;
        var sawDailyMeasurement = false;

        foreach (var downloadMeasurement in request.Measurements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filters = new List<DataFileFilterAndAdjustment> { downloadMeasurement.FileFilter };
            var records = downloadMeasurement.MeasurementDefinition.RowDataType switch
            {
                RowDataType.OneValuePerRow => await DataReaderFunctions.GetDataRecords(
                    downloadMeasurement.MeasurementDefinition,
                    filters,
                    temporaryDirectory),
                RowDataType.TwelveMonthsPerRow => await TwelveMonthPerLineDataReader.GetTwelveMonthsPerRowData(
                    downloadMeasurement.MeasurementDefinition,
                    filters,
                    temporaryDirectory),
                _ => throw new NotSupportedException($"No dataset validation reader exists for {downloadMeasurement.MeasurementDefinition.RowDataType}."),
            };

            if (records.Count == 0 || !records.Any(x => x.Value.HasValue && double.IsFinite(x.Value.Value)))
            {
                throw new InvalidDataException($"Downloaded source '{request.AssetKey}' contained no finite measurements.");
            }

            if (downloadMeasurement.MeasurementDefinition.DataResolution == DataResolution.Daily)
            {
                sawDailyMeasurement = true;
                var measurementLatestDate = records
                    .Where(x => x.Value.HasValue && double.IsFinite(x.Value.Value) && x.Date.HasValue)
                    .Max(x => x.Date);
                if (measurementLatestDate.HasValue && (latestRecordDate is null || measurementLatestDate.Value < latestRecordDate.Value))
                {
                    latestRecordDate = measurementLatestDate.Value;
                }
            }
        }

        return sawDailyMeasurement ? latestRecordDate : null;
    }
}
