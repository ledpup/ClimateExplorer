namespace ClimateExplorer.Web.Client.Components.ChartSeries;

using System.Globalization;
using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

public partial class AboutDataDetails
{
    [Parameter]
    public DataSetDefinitionViewModel? DataSetDefinition { get; set; }

    [Parameter]
    public DataSetMetadata? Metadata { get; set; }

    private static bool HasRenderableMetadata(DataSetMetadata metadata)
    {
        return !string.IsNullOrWhiteSpace(metadata.SourceCode) ||
            !string.IsNullOrWhiteSpace(metadata.SourceName) ||
            !string.IsNullOrWhiteSpace(metadata.SourceUrl) ||
            HasRenderableStations(metadata);
    }

    private static bool HasRenderableStations(DataSetMetadata metadata)
    {
        return metadata.Stations.Any(
            x => !string.IsNullOrWhiteSpace(x.StationId) ||
                !string.IsNullOrWhiteSpace(x.StationName));
    }

    private static bool HasStationLinks(DataSetMetadata metadata)
    {
        return metadata.Stations.Any(x => !string.IsNullOrWhiteSpace(x.SourceUrl));
    }

    private static List<MeasurementDefinitionViewModel>? GetMeasurementDefinitions(DataSetDefinitionViewModel? dataSetDefinition)
    {
        return dataSetDefinition?.MeasurementDefinitions;
    }

    private static string FormatDataType(MeasurementDefinitionViewModel measurementDefinition)
    {
        return measurementDefinition.DataType.ToFriendlyName();
    }

    private static string FormatDataResolution(MeasurementDefinitionViewModel measurementDefinition)
    {
        return measurementDefinition.DataResolution.ToString();
    }

    private static string FormatUnitOfMeasure(MeasurementDefinitionViewModel measurementDefinition)
    {
        return Enums.UnitOfMeasureLabelShort(measurementDefinition.UnitOfMeasure);
    }

    private static string FormatDataAdjustment(MeasurementDefinitionViewModel measurementDefinition)
    {
        return measurementDefinition.DataAdjustment?.ToString() ?? "\u2014";
    }

    private static string FormatStation(DataSetStationMetadata station)
    {
        if (!string.IsNullOrWhiteSpace(station.StationName) &&
            !string.IsNullOrWhiteSpace(station.StationId))
        {
            return $"{station.StationName} ({station.StationId})";
        }

        return FormatMissing(station.StationName ?? station.StationId);
    }

    private static string FormatStationDateRange(DataSetStationMetadata station)
    {
        return $"{FormatDate(station.StationStartDate)} to {FormatDate(station.StationEndDate)}";
    }

    private static string FormatDate(DateOnly? date)
    {
        return date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—";
    }

    private static string FormatMissing(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "\u2014" : value;
    }

    private static string? GetStationOrSourceUrl(DataSetStationMetadata station, DataSetMetadata metadata)
    {
        return !string.IsNullOrWhiteSpace(station.SourceUrl)
            ? station.SourceUrl
            : metadata.SourceUrl;
    }

    private static string FormatStationOrSourceUrlLabel(DataSetStationMetadata station, DataSetMetadata metadata)
    {
        return !string.IsNullOrWhiteSpace(station.SourceUrl)
            ? FormatStationUrlLabel(station)
            : FormatSourceUrlLabel(metadata);
    }

    private static string FormatStationUrlLabel(DataSetStationMetadata station)
    {
        if (!string.IsNullOrWhiteSpace(station.SourceUrlLabel))
        {
            return station.SourceUrlLabel;
        }

        return !string.IsNullOrWhiteSpace(station.StationId)
            ? $"Station {station.StationId}"
            : "Source";
    }

    private static string FormatSourceUrlLabel(DataSetMetadata metadata)
    {
        return string.IsNullOrWhiteSpace(metadata.SourceUrlLabel)
            ? "Source"
            : metadata.SourceUrlLabel;
    }
}
