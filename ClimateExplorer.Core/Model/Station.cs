namespace ClimateExplorer.Core.Model;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Station
{
    public required string Id { get; set; }
    public string? Name { get; set; }
    public string? CountryCode { get; set; }

    /// <summary>
    /// The station's WMO number, where NOAA's ghcnd-stations.txt publishes one (columns 81-85). Only a
    /// minority of GHCN stations carry one. It is the deterministic join key to WIGOS-style station ids
    /// ("0-20000-0-{wmoId:00000}") used by EUMETNET's blended collections; the non-blended ECA&D
    /// collection exposes no WMO id at all, so <see cref="ClimateExplorer.Core"/>'s ECA&D integration
    /// links stations by coordinate and name instead.
    /// </summary>
    public string? WmoId { get; set; }

    public Coordinates? Coordinates { get; set; }
    public int? FirstYear { get; set; }
    public int? LastYear { get; set; }
    public int? YearsOfMissingData { get; set; }

    [JsonIgnore]
    public int? Age => LastYear - FirstYear + 1;

    [JsonIgnore]
    public List<StationDistance>? StationDistances { get; set; }

    [JsonIgnore]
    public double? AverageDistance { get; set; }

    [JsonIgnore]
    public int? Score
    {
        get
        {
            return Age - YearsOfMissingData;
        }
    }

    [JsonIgnore]
    public string? Source { get; set; }

    public static async Task<List<Station>> GetStationsFromFiles(List<string> pathAndFileNames)
    {
        var stations = new List<Station>();
        foreach (var pathAndFileName in pathAndFileNames)
        {
            var list = await GetStationsFromFile(pathAndFileName);
            stations.AddRange(list!);
        }

        return stations;
    }

    public static async Task<List<Station>> GetStationsFromFile(string pathAndFileName)
    {
        var text = await File.ReadAllTextAsync(pathAndFileName);
        var list = JsonSerializer.Deserialize<List<Station>>(text)!;
        return list;
    }

    public override string ToString()
    {
        return Id;
    }
}
