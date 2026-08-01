namespace ClimateExplorer.Data.Ghcnd;

using System.Globalization;

/// <summary>
/// Reads NOAA's fixed-width <c>ghcnd-stations.txt</c>. Its layout is documented in this project's
/// <c>readme.txt</c> under "FORMAT OF ghcnd-stations.txt".
/// <para>
/// The WMO id is the interesting field: it is the deterministic join key to the WIGOS-style station ids
/// (<c>0-20000-0-{wmoId:00000}</c>) that EUMETNET's blended collections use, so populating
/// <see cref="ClimateExplorer.Core.Model.Station.WmoId"/> from here is what a blended ECA&amp;D integration
/// would link on. Only a minority of stations publish one.
/// </para>
/// </summary>
public static class GhcndStationFile
{
    // Columns are 1-based in the documentation and inclusive at both ends.
    private const int IdStart = 0;
    private const int IdLength = 11;
    private const int LatitudeStart = 12;
    private const int LatitudeLength = 8;
    private const int LongitudeStart = 21;
    private const int LongitudeLength = 9;
    private const int NameStart = 41;
    private const int NameLength = 30;
    private const int WmoIdStart = 80;
    private const int WmoIdLength = 5;

    public const string DefaultFileName = "ghcnd-stations.txt";

    public static async Task<IReadOnlyList<GhcndStationRow>> ReadAsync(string pathAndFileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathAndFileName);

        var rows = new List<GhcndStationRow>();
        foreach (var line in await File.ReadAllLinesAsync(pathAndFileName, cancellationToken))
        {
            if (TryParse(line, out var row))
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    public static bool TryParse(string line, out GhcndStationRow row)
    {
        row = default!;
        if (string.IsNullOrWhiteSpace(line) || line.Length < WmoIdStart)
        {
            return false;
        }

        var id = Read(line, IdStart, IdLength);
        if (id.Length == 0 ||
            !double.TryParse(Read(line, LatitudeStart, LatitudeLength), NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
            !double.TryParse(Read(line, LongitudeStart, LongitudeLength), NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
        {
            return false;
        }

        var wmoId = Read(line, WmoIdStart, WmoIdLength);
        row = new GhcndStationRow(
            id,
            Read(line, NameStart, NameLength),
            latitude,
            longitude,
            wmoId.Length == 0 ? null : wmoId);
        return true;
    }

    private static string Read(string line, int start, int length)
    {
        if (start >= line.Length)
        {
            return string.Empty;
        }

        return line.Substring(start, Math.Min(length, line.Length - start)).Trim();
    }
}

public sealed record GhcndStationRow(string Id, string Name, double Latitude, double Longitude, string? WmoId);
