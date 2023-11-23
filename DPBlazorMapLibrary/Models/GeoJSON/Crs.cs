using System.Text.Json.Serialization;

namespace DPBlazorMapLibrary;

public class Crs
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("properties")]
    public object? Properties { get; set; }
}