namespace ClimateExplorer.Data.Ecad;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// The GHCN station id to ECA&amp;D station id crosswalk, checked in beside the other metadata and read by
/// the runtime downloader.
/// <para>
/// Everything ClimateExplorer stores about an ECA&amp;D-sourced location - the location mapping, the source
/// file name, the station metadata lookup - is keyed by GHCN station id, exactly as the other GHCN-family
/// datasets are, so ECA&amp;D's own <c>ecad_XXXXXXX</c> id never leaks past the API boundary. This file is
/// where the translation happens, and it is the only place ECA&amp;D ids appear.
/// </para>
/// </summary>
public static class EcadStationCrosswalk
{
    public const string FileName = "EcadNonBlendedStationIds.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task<IReadOnlyDictionary<string, string>> LoadAsync(string pathAndFileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathAndFileName);

        if (!File.Exists(pathAndFileName))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        await using var stream = File.OpenRead(pathAndFileName);
        var crosswalk = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, SerializerOptions, cancellationToken);
        return crosswalk ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public static async Task SaveAsync(
        string pathAndFileName,
        IEnumerable<EcadStationMatch> matches,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathAndFileName);
        ArgumentNullException.ThrowIfNull(matches);

        var crosswalk = matches
            .OrderBy(x => x.GhcnStationId, StringComparer.Ordinal)
            .ToDictionary(x => x.GhcnStationId, x => x.EcadStationId, StringComparer.Ordinal);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pathAndFileName))!);
        await File.WriteAllTextAsync(
            pathAndFileName,
            JsonSerializer.Serialize(crosswalk, SerializerOptions),
            cancellationToken);
    }
}
