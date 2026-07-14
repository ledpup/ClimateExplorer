namespace ClimateExplorer.Data.Downloading.Downloaders;

using System.Globalization;
using System.Text.Json;

public sealed class GreenlandMeltDataClient(HttpClient httpClient)
{
    private readonly HttpClient httpClient = httpClient;

    public async Task<IReadOnlyDictionary<DateOnly, double?>> GetYearAsync(int year, CancellationToken cancellationToken)
    {
        var url = $"https://nsidc.org/api/greenland/melt_area/{year}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        Dictionary<string, double?>? raw;
        try
        {
            raw = JsonSerializer.Deserialize<Dictionary<string, double?>>(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Greenland melt data for {year} was not valid JSON.", ex);
        }

        if (raw == null || raw.Count == 0)
        {
            throw new InvalidDataException($"Greenland melt data for {year} was empty.");
        }

        var byDate = new Dictionary<DateOnly, double?>();
        foreach (var (key, value) in raw)
        {
            if (key.Length < 10 || !DateOnly.TryParseExact(key[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                throw new InvalidDataException($"Greenland melt data for {year} contained an unrecognised date key '{key}'.");
            }

            byDate[date] = value;
        }

        return byDate;
    }
}
