namespace ClimateExplorer.Data.Downloading.Downloaders;

using System.IO.Compression;
using System.Text.RegularExpressions;

public sealed partial class BomDailyDataClient(HttpClient httpClient)
{
    private const int MaximumDownloadBytes = 100 * 1024 * 1024;
    private readonly HttpClient httpClient = httpClient;

    public async Task<string> DownloadCsvAsync(
        string stationId,
        BomDailyObservationCode observationCode,
        CancellationToken cancellationToken)
    {
        var availableYearsUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/wData/wdata?p_stn_num={stationId}&p_display_type=availableYears&p_nccObsCode={(int)observationCode}";
        using var availableYearsResponse = await httpClient.GetAsync(availableYearsUrl, cancellationToken);
        availableYearsResponse.EnsureSuccessStatusCode();
        var availableYearsContent = await availableYearsResponse.Content.ReadAsStringAsync(cancellationToken);
        var match = DailyBomAvailableYearsRegex().Match(availableYearsContent);
        if (!match.Success)
        {
            throw new InvalidDataException($"BOM did not return a download token for station '{stationId}' and observation code '{(int)observationCode}'.");
        }

        var zipFileUrl = $"http://www.bom.gov.au/jsp/ncc/cdio/weatherData/av?p_display_type=dailyZippedDataFile&p_stn_num={stationId}&p_nccObsCode={(int)observationCode}&p_c={match.Groups["p_c"].Value}";
        using var zipResponse = await httpClient.GetAsync(zipFileUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        zipResponse.EnsureSuccessStatusCode();
        if (zipResponse.Content.Headers.ContentLength > MaximumDownloadBytes)
        {
            throw new InvalidDataException("BOM station download exceeded the size limit.");
        }

        await using var zipStream = await zipResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);
        var csvEntries = archive.Entries.Where(x => x.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)).ToList();
        if (csvEntries.Count != 1 || csvEntries[0].Length > MaximumDownloadBytes)
        {
            throw new InvalidDataException($"BOM station archive contained {csvEntries.Count} CSV entries or exceeded the size limit.");
        }

        await using var entryStream = csvEntries[0].Open();
        using var reader = new StreamReader(entryStream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidDataException("BOM station archive contained an empty CSV file.");
        }

        return content;
    }

    [GeneratedRegex(@"\d{6}\|\|,(?<startYear>\d{4}):(?<p_c>-?\d+),")]
    private static partial Regex DailyBomAvailableYearsRegex();
}
