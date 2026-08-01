namespace ClimateExplorer.Data.Ecad;

using System.Text.Json;
using System.Text.Json.Serialization;
using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;

/// <summary>
/// Writes ECA&amp;D's location coverage file. Nothing about the GHCNd mapping files is touched: ECA&amp;D
/// takes precedence over GHCNd purely by being declared first in
/// <see cref="DataSetDefinitionsBuilder.BuildDataSetDefinitions"/>, which keeps this whole integration
/// additive and reversible.
/// </summary>
public static class EcadDataFileMappingBuilder
{
    public const string FileName = "DataFileMapping_ecad_unadjusted.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task CreateDataFileMappingAsync(
        IEnumerable<EcadStationMatch> matches,
        IReadOnlyDictionary<string, Guid> ghcnIdToLocationIds,
        string pathAndFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(ghcnIdToLocationIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(pathAndFileName);

        var dataFileMapping = new DataFileMapping
        {
            DataSetDefinitionId = DataSetDefinitionsBuilder.EcadDataSetDefinitionId,
            LocationIdToDataFileMappings = [],
        };

        foreach (var match in matches.OrderBy(x => x.GhcnStationId, StringComparer.Ordinal))
        {
            if (!ghcnIdToLocationIds.TryGetValue(match.GhcnStationId, out var locationId))
            {
                throw new InvalidOperationException(
                    $"GHCN station '{match.GhcnStationId}' has no location id; run ClimateExplorer.Data.Ghcnm first.");
            }

            dataFileMapping.LocationIdToDataFileMappings.Add(locationId, [new() { Id = match.GhcnStationId }]);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pathAndFileName))!);
        await File.WriteAllTextAsync(
            pathAndFileName,
            JsonSerializer.Serialize(dataFileMapping, SerializerOptions),
            cancellationToken);
    }

    public static async Task<IReadOnlyDictionary<string, Guid>> GetGhcnIdToLocationIdsAsync(CancellationToken cancellationToken = default)
    {
        const string ghcnIdToLocationIdsFile = Folders.GhcnmFolder + @"MetaData\GhcnIdToLocationIds.json";
        if (!File.Exists(ghcnIdToLocationIdsFile))
        {
            throw new FileNotFoundException($"Expecting {ghcnIdToLocationIdsFile} to exist", ghcnIdToLocationIdsFile);
        }

        var contents = await File.ReadAllTextAsync(ghcnIdToLocationIdsFile, cancellationToken);
        return JsonSerializer.Deserialize<Dictionary<string, Guid>>(contents)!;
    }
}
