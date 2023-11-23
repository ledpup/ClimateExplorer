using System.Text.Json.Serialization;

namespace DPBlazorMapLibrary;

public class GeoJSON
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("crs")]
    public Crs? Crs { get; set; }

    [JsonPropertyName("features")]
    public List<Feature>? Features { get; set; }

    public static GeoJSON DeserealizeFromString(string json)
    {
        var geoJson = System.Text.Json.JsonSerializer.Deserialize<GeoJSON>(json)!;
        return geoJson;
    }

    public static async Task<GeoJSON> DeserealizeFromStringAsync(Stream jsonStream)
    {
        var geoJson = await System.Text.Json.JsonSerializer.DeserializeAsync<GeoJSON>(jsonStream);
        return geoJson!;
    }
}
