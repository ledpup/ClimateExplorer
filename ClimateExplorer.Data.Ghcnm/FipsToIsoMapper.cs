namespace ClimateExplorer.Data.Ghcnm;

/// <summary>
/// Maps a GHCNm station to an ISO 3166-1 alpha-2 country code. GHCNm station IDs start with a FIPS 10-4
/// country code; FIPS codes must not leave this project.
/// </summary>
public class FipsToIsoMapper
{
    public const string MappingFile = @"MetaData\ghcnm-fips-to-iso.txt";
    public const string StationOverridesFile = @"MetaData\ghcnm-station-iso-overrides.txt";

    // A FIPS code that can't be mapped as a whole; each of its stations needs an override.
    private const string Unmappable = "--";

    private readonly Dictionary<string, string> fipsToIso;
    private readonly Dictionary<string, string> stationOverrides;

    public FipsToIsoMapper(Dictionary<string, string> fipsToIso, Dictionary<string, string> stationOverrides)
    {
        this.fipsToIso = fipsToIso;
        this.stationOverrides = stationOverrides;
    }

    public IEnumerable<string> IsoCodes =>
        fipsToIso.Values
            .Concat(stationOverrides.Values)
            .Where(x => x != Unmappable)
            .Distinct();

    public static async Task<FipsToIsoMapper> Load(string mappingFile = MappingFile, string stationOverridesFile = StationOverridesFile)
    {
        return new FipsToIsoMapper(await ReadCodes(mappingFile), await ReadCodes(stationOverridesFile));
    }

    public bool TryMap(string stationId, out string isoCode)
    {
        if (stationOverrides.TryGetValue(stationId, out var overrideCode))
        {
            isoCode = overrideCode;
            return true;
        }

        if (stationId.Length >= 2
            && fipsToIso.TryGetValue(stationId[..2], out var mappedCode)
            && mappedCode != Unmappable)
        {
            isoCode = mappedCode;
            return true;
        }

        isoCode = string.Empty;
        return false;
    }

    // Lines are "<key> <ISO code> <name>". The name is only there for reviewers. Lines starting with # are comments.
    public static async Task<Dictionary<string, string>> ReadCodes(string path)
    {
        var codes = new Dictionary<string, string>();
        foreach (var line in await File.ReadAllLinesAsync(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var columns = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length < 2 || columns[1].Length != 2)
            {
                throw new FormatException($"Line '{line}' in {path} is not '<key> <ISO code> <name>'");
            }

            if (!codes.TryAdd(columns[0], columns[1]))
            {
                throw new FormatException($"{columns[0]} appears more than once in {path}");
            }
        }

        return codes;
    }
}
