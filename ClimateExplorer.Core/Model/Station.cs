using ClimateExplorer.Core.Model;
using System.Text.Json;

public class Station
{
    public string ExternalStationCode { get; set; }
    public string Name { get; set; }
    public Coordinates? Coordinates { get; set; }
    public DateTime? Opened { get; set; }
    public DateTime? Closed { get; set; }

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
