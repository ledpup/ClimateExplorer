using System.Text.Json.Serialization;

namespace DPBlazorMapLibrary;

public class Geometry
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("coordinates")]
    public object[][][]? Coordinates { get; set; }
}