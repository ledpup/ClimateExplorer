namespace ClimateExplorer.Web.Client.Components.ChartSeries;

using System.Globalization;
using Blazorise;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

public partial class AboutData
{
    private Modal? modal;

    [Parameter]
    public ChartSeriesDefinition? ChartSeries { get; set; }

    [Parameter]
    public IReadOnlyList<DataSetSourceMetadata>? SourceMetadata { get; set; }

    public Task Show()
    {
        return modal!.Show();
    }

    private static bool HasRenderableSourceMetadata(DataSetSourceMetadata metadata)
    {
        return !string.IsNullOrWhiteSpace(metadata.SourceCode) ||
            !string.IsNullOrWhiteSpace(metadata.SourceName) ||
            !string.IsNullOrWhiteSpace(metadata.SourceUrl) ||
            HasRenderableStations(metadata);
    }

    private static bool HasRenderableStations(DataSetSourceMetadata metadata)
    {
        return metadata.Stations.Any(
            x => !string.IsNullOrWhiteSpace(x.StationId) ||
                !string.IsNullOrWhiteSpace(x.StationName));
    }

    private static bool IsSingleStation(DataSetSourceMetadata metadata)
    {
        return metadata.Stations.Count == 1 &&
            (!string.IsNullOrWhiteSpace(metadata.Stations[0].StationId) ||
                !string.IsNullOrWhiteSpace(metadata.Stations[0].StationName));
    }

    private static bool HasStationLinks(DataSetSourceMetadata metadata)
    {
        return metadata.Stations.Any(x => !string.IsNullOrWhiteSpace(x.SourceUrl));
    }

    private static string FormatSource(DataSetSourceMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.SourceCode) &&
            !string.IsNullOrWhiteSpace(metadata.SourceName) &&
            !string.Equals(metadata.SourceCode, metadata.SourceName, StringComparison.OrdinalIgnoreCase))
        {
            return $"{metadata.SourceCode} - {metadata.SourceName}";
        }

        return FormatMissing(metadata.SourceCode ?? metadata.SourceName);
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
        return date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "Unknown";
    }

    private static string FormatMissing(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "\u2014" : value;
    }

    private static string? GetStationOrSourceUrl(DataSetStationMetadata station, DataSetSourceMetadata metadata)
    {
        return !string.IsNullOrWhiteSpace(station.SourceUrl)
            ? station.SourceUrl
            : metadata.SourceUrl;
    }

    private static string FormatStationOrSourceUrlLabel(DataSetStationMetadata station, DataSetSourceMetadata metadata)
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

    private static string FormatSourceUrlLabel(DataSetSourceMetadata metadata)
    {
        return string.IsNullOrWhiteSpace(metadata.SourceUrlLabel)
            ? "Source"
            : metadata.SourceUrlLabel;
    }

    private DataSetSourceMetadata? FindSourceMetadata(SourceSeriesSpecification sourceSeriesSpecification)
    {
        return SourceMetadata?.FirstOrDefault(
            x => x.DataSetDefinitionId == sourceSeriesSpecification.DataSetDefinition?.Id &&
                x.LocationId == sourceSeriesSpecification.LocationId);
    }
}
