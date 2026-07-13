namespace ClimateExplorer.Data.Downloading;

using ClimateExplorer.Core.Model;

public sealed record DataSetDownloadMeasurement(
    MeasurementDefinition MeasurementDefinition,
    DataFileFilterAndAdjustment FileFilter);
