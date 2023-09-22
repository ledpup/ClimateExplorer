using ClimateExplorer.Core.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Station
{
    public required string Id { get; set; }
    public string? Name { get; set; }
    public string? CountryCode { get; set; }
    public Coordinates? Coordinates { get; set; }
    public DateOnly? FirstYear { get; set; }
    public DateOnly? LastYear { get; set; }
    public int? YearsOfMissingData { get; set; }

    [JsonIgnore]
    public int? Age => LastYear?.Year - FirstYear?.Year;

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

    public static async Task<List<Station>?> GetStationsFromFiles(List<string> pathAndFileNames)
    {
        var stations = new List<Station>();
        foreach (var pathAndFileName in pathAndFileNames)
        {
            var text = await File.ReadAllTextAsync(pathAndFileName);
            var list = JsonSerializer.Deserialize<List<Station>>(text);
            stations.AddRange(list);
        }
        return stations;
    }
}
