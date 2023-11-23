using System.Text.Json.Serialization;

namespace DPBlazorMapLibrary;

public class Feature
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("properties")]
    public object? Properties { get; set; }

    [JsonPropertyName("geometry")]
    public Geometry? Geometry { get; set; }
}