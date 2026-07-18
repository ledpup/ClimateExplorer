namespace ClimateExplorer.Data.Downloading.Models;

using ClimateExplorer.Core.Model;

public sealed record DataSetDownloadMeasurement(
    MeasurementDefinition MeasurementDefinition,
    DataFileFilterAndAdjustment FileFilter);
