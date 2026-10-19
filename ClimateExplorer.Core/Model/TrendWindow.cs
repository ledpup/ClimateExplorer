namespace ClimateExplorer.Core.Model;

using System.Text.Json.Serialization;

/// <summary>
/// Identifies which period of a series a trend is fitted over. Serialised by name so the
/// cached/served JSON says which window each trend is.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TrendWindow>))]
public enum TrendWindow
{
    Full,
    Last30,
    RecentDecade,
    FirstHalf,
}
