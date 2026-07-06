#nullable enable

namespace ClimateExplorer.WebApi.Metadata;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Private helpers are kept below the public flow they support.")]
internal sealed class DataSetSourceMetadataBuilder
{
    private readonly IStationMetadataLookup stationMetadataLookup;
    private readonly Func<Guid, Task<GeographicalEntity>> getGeographicalEntity;

    public DataSetSourceMetadataBuilder(
        IStationMetadataLookup? stationMetadataLookup = null,
        Func<Guid, Task<GeographicalEntity>>? getGeographicalEntity = null)
    {
        this.stationMetadataLookup = stationMetadataLookup ?? new StationMetadataLookup();
        this.getGeographicalEntity = getGeographicalEntity ?? GeographicalEntity.GetGeographicalEntity;
    }

    public async Task<List<DataSetSourceMetadata>> BuildAsync(
        PostDataSetsRequestBody body,
        IReadOnlyList<DataSetDefinition>? dataSetDefinitions = null)
    {
        if (body.SeriesSpecifications is null || body.SeriesSpecifications.Length == 0)
        {
            return [];
        }

        dataSetDefinitions ??= await DataSetDefinition.GetDataSetDefinitions();

        var sourceMetadata = new List<DataSetSourceMetadata>();
        foreach (var specification in body.SeriesSpecifications)
        {
            var dataSetDefinition = dataSetDefinitions.Single(x => x.Id == specification.DataSetDefinitionId);
            var geographicalEntity = await getGeographicalEntity(specification.LocationId);
            var mappings = GetDataFileMappings(dataSetDefinition, specification.LocationId);

            sourceMetadata.Add(new DataSetSourceMetadata
            {
                DataSetDefinitionId = dataSetDefinition.Id,
                LocationId = geographicalEntity.Id,
                LocationName = geographicalEntity.Name,
                SourceCode = dataSetDefinition.ShortName,
                SourceName = dataSetDefinition.Name,
                SourceUrl = ResolveSourceUrl(dataSetDefinition, mappings),
                SourceUrlLabel = dataSetDefinition.ShortName,
                Stations = await BuildStationsAsync(dataSetDefinition, mappings),
            });
        }

        return sourceMetadata;
    }

    private static List<DataFileFilterAndAdjustment> GetDataFileMappings(
        DataSetDefinition dataSetDefinition,
        Guid locationId)
    {
        return dataSetDefinition.DataLocationMapping?.LocationIdToDataFileMappings.TryGetValue(locationId, out var mappings) == true
            ? mappings
            : [];
    }

    private async Task<List<DataSetStationMetadata>> BuildStationsAsync(
        DataSetDefinition dataSetDefinition,
        List<DataFileFilterAndAdjustment> mappings)
    {
        if (!IsStationBacked(dataSetDefinition))
        {
            return [];
        }

        var stations = new List<DataSetStationMetadata>();
        foreach (var mapping in mappings)
        {
            var station = await stationMetadataLookup.GetStationAsync(dataSetDefinition, mapping.Id);
            stations.Add(new DataSetStationMetadata
            {
                StationId = mapping.Id,
                StationName = station?.Name,
                StationStartDate = mapping.StartDate ?? CreateFirstDayOfYear(station?.FirstYear),
                StationEndDate = mapping.EndDate ?? CreateLastDayOfYear(station?.LastYear),
                SourceUrl = ResolveStationUrl(dataSetDefinition, mapping.Id),
                SourceUrlLabel = $"Station {mapping.Id}",
            });
        }

        return stations;
    }

    private static bool IsStationBacked(DataSetDefinition dataSetDefinition)
    {
        return !string.IsNullOrWhiteSpace(dataSetDefinition.StationMetadataFileName) ||
            ContainsStationToken(dataSetDefinition.StationInfoUrl) ||
            ContainsStationToken(dataSetDefinition.LocationInfoUrl) ||
            ContainsStationToken(dataSetDefinition.DataDownloadUrl);
    }

    private static DateOnly? CreateFirstDayOfYear(int? year)
    {
        return year.HasValue ? new DateOnly(year.Value, 1, 1) : null;
    }

    private static DateOnly? CreateLastDayOfYear(int? year)
    {
        return year.HasValue ? new DateOnly(year.Value, 12, 31) : null;
    }

    private static string? ResolveSourceUrl(
        DataSetDefinition dataSetDefinition,
        List<DataFileFilterAndAdjustment> mappings)
    {
        if (!string.IsNullOrWhiteSpace(dataSetDefinition.LocationInfoUrl) && mappings.Count > 0)
        {
            return ExpandUrlTemplate(dataSetDefinition.LocationInfoUrl, mappings[0].Id);
        }

        return FirstUsableUrl(
            dataSetDefinition.MoreInformationUrl,
            dataSetDefinition.PublisherUrl,
            dataSetDefinition.DataDownloadUrl);
    }

    private static string? ResolveStationUrl(DataSetDefinition dataSetDefinition, string stationId)
    {
        return FirstUsableUrl(
            ExpandUrlTemplate(dataSetDefinition.StationInfoUrl, stationId),
            ExpandUrlTemplate(dataSetDefinition.DataDownloadUrl, stationId));
    }

    private static string? ExpandUrlTemplate(string? template, string stationId)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        var url = template
            .Replace("[station]", stationId, StringComparison.OrdinalIgnoreCase)
            .Replace("[primaryStation]", stationId, StringComparison.OrdinalIgnoreCase);

        return url.Contains('[') ? null : url;
    }

    private static string? FirstUsableUrl(params string?[] urls)
    {
        return urls.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && !x.Contains('['));
    }

    private static bool ContainsStationToken(string? value)
    {
        return value?.Contains("[station]", StringComparison.OrdinalIgnoreCase) == true ||
            value?.Contains("[primaryStation]", StringComparison.OrdinalIgnoreCase) == true;
    }
}
