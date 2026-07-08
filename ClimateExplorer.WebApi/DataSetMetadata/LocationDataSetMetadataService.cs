#nullable enable

namespace ClimateExplorer.WebApi.Metadata;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;

internal sealed class LocationDataSetMetadataService(
    Func<Task<IEnumerable<Location>>>? getLocations = null,
    Func<Task<List<DataSetDefinition>>>? getDataSetDefinitions = null,
    DataSetMetadataBuilder? sourceMetadataBuilder = null)
{
    private readonly Func<Task<IEnumerable<Location>>> getLocations = getLocations ?? (async () => await Location.GetLocations());
    private readonly Func<Task<List<DataSetDefinition>>> getDataSetDefinitions = getDataSetDefinitions ?? DataSetDefinition.GetDataSetDefinitions;
    private readonly DataSetMetadataBuilder sourceMetadataBuilder = sourceMetadataBuilder ?? new DataSetMetadataBuilder();

    public async Task<LocationDataSetMetadataResult> GetAsync(Guid locationId)
    {
        var locations = await getLocations();
        if (!locations.Any(x => x.Id == locationId))
        {
            return LocationDataSetMetadataResult.NotFound();
        }

        var dataSetDefinitions = await getDataSetDefinitions();
        var availableDefinitions = dataSetDefinitions
            .Where(x => x.DataLocationMapping?.LocationIdToDataFileMappings.ContainsKey(locationId) == true)
            .OrderBy(x => x.ShortName)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .ToList();

        var sourceMetadata = new List<DataSetMetadata>();
        foreach (var dataSetDefinition in availableDefinitions)
        {
            sourceMetadata.Add(await sourceMetadataBuilder.BuildAsync(dataSetDefinition, locationId));
        }

        return LocationDataSetMetadataResult.Found(sourceMetadata);
    }
}
