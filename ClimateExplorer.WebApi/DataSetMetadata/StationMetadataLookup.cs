#nullable enable

namespace ClimateExplorer.WebApi.Metadata;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Private helpers are kept below the public flow they support.")]
internal sealed class StationMetadataLookup : IStationMetadataLookup
{
    private static readonly ConcurrentDictionary<string, Task<IReadOnlyDictionary<string, Station>>> StationsByFileName = new();

    public async Task<Station?> GetStationAsync(DataSetDefinition dataSetDefinition, string stationId)
    {
        if (string.IsNullOrWhiteSpace(dataSetDefinition.StationMetadataFileName) ||
            string.IsNullOrWhiteSpace(stationId))
        {
            return null;
        }

        var stations = await GetStationsAsync(dataSetDefinition.StationMetadataFileName);
        return stations.TryGetValue(stationId, out var station) ? station : null;
    }

    private Task<IReadOnlyDictionary<string, Station>> GetStationsAsync(string stationMetadataFileName)
    {
        return StationsByFileName.GetOrAdd(stationMetadataFileName, LoadStationsAsync);
    }

    private static async Task<IReadOnlyDictionary<string, Station>> LoadStationsAsync(string stationMetadataFileName)
    {
        var path = Path.Combine("MetaData", "Station", stationMetadataFileName);
        if (!File.Exists(path))
        {
            return new Dictionary<string, Station>(StringComparer.OrdinalIgnoreCase);
        }

        var stations = await Station.GetStationsFromFile(path);
        return stations
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    }
}
