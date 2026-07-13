namespace ClimateExplorer.Data.Downloading;

using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public sealed class DataSetDownloadValidator
{
    public async Task ValidateAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
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
        }
    }
}
