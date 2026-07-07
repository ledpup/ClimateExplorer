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
    public IReadOnlyList<DataSetMetadata>? SourceMetadata { get; set; }

    public Task Show()
    {
        return modal!.Show();
    }

    private static bool HasRenderableSourceMetadata(DataSetMetadata metadata)
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

    private static bool IsSingleStation(DataSetMetadata metadata)
    {
        return metadata.Stations.Count == 1 &&
            (!string.IsNullOrWhiteSpace(metadata.Stations[0].StationId) ||
                !string.IsNullOrWhiteSpace(metadata.Stations[0].StationName));
    }

    private static bool HasStationLinks(DataSetMetadata metadata)
    {
        return metadata.Stations.Any(x => !string.IsNullOrWhiteSpace(x.SourceUrl));
    }

    private static string FormatSource(DataSetMetadata metadata)
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

    private DataSetMetadata? FindSourceMetadata(SourceSeriesSpecification sourceSeriesSpecification)
    {
        return SourceMetadata?.FirstOrDefault(
            x => x.DataSetDefinitionId == sourceSeriesSpecification.DataSetDefinition?.Id &&
                x.LocationId == sourceSeriesSpecification.LocationId);
    }
}
