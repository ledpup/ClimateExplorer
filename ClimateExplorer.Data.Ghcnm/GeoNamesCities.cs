using ClimateExplorer.Core.Model;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO.Compression;

namespace ClimateExplorer.Data.Ghcnm;

/// <summary>
/// Cities from GeoNames (CC BY 4.0) for the city rule. The repo keeps a filtered extract, so runs can be repeated
/// and location names don't change when GeoNames does. If the extract is missing, it's rebuilt from the current
/// GeoNames download; delete it to refresh.
/// </summary>
public static class GeoNamesCities
{
    public const string ExtractFile = @"MetaData\geonames-cities.tsv";

    /// <summary>The extract holds cities of at least this many people, so the city rule's threshold can't go lower.</summary>
    public const int ExtractMinimumPopulation = 50_000;

    // The project's copy (the app runs from bin), so a rebuilt extract can be committed.
    private const string ProjectExtractFile = @"..\..\..\MetaData\geonames-cities.tsv";
    private const string DownloadUrl = "https://download.geonames.org/export/dump/cities15000.zip";
    private const string DownloadFile = @"Download\cities15000.zip";
    private const string DownloadEntry = "cities15000.txt";

    // Sections of a populated place (suburbs, which would each count as a city), and historical, abandoned and
    // destroyed places.
    private static readonly HashSet<string> ExcludedFeatureCodes = ["PPLX", "PPLH", "PPLQ", "PPLW"];

    public static async Task<List<City>> Load(HttpClient httpClient, int minimumPopulation, ILogger logger)
    {
        if (minimumPopulation < ExtractMinimumPopulation)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumPopulation), $"{ExtractFile} only holds cities of {ExtractMinimumPopulation} or more.");
        }

        if (!File.Exists(ExtractFile))
        {
            await CreateExtract(httpClient, logger);
        }

        var cities = Parse(await File.ReadAllLinesAsync(ExtractFile))
            .Where(x => x.Population >= minimumPopulation)
            .ToList();

        logger.LogInformation($"{cities.Count} cities of {minimumPopulation} or more read from {ExtractFile}");

        return cities;
    }

    // Columns: GeoNames ID, ASCII name, latitude, longitude, country code, population. Lines starting with # are comments.
    static IEnumerable<City> Parse(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var columns = line.Split('\t');
            yield return new City(
                columns[1],
                columns[4],
                new Coordinates(double.Parse(columns[2], CultureInfo.InvariantCulture), double.Parse(columns[3], CultureInfo.InvariantCulture)),
                int.Parse(columns[5], CultureInfo.InvariantCulture));
        }
    }

    static async Task CreateExtract(HttpClient httpClient, ILogger logger)
    {
        Directory.CreateDirectory("Download");
        if (!File.Exists(DownloadFile))
        {
            logger.LogInformation($"{ExtractFile} not found. Downloading {DownloadUrl} to rebuild it");
            var bytes = await httpClient.GetByteArrayAsync(DownloadUrl);
            await File.WriteAllBytesAsync(DownloadFile, bytes);
        }

        // cities15000.txt columns: 0 geonameid, 1 name, 2 asciiname, 4 latitude, 5 longitude, 7 feature code,
        // 8 country code, 14 population.
        var rows = new List<(int Population, long Id, string Line)>();
        using (var archive = await ZipFile.OpenReadAsync(DownloadFile))
        {
            var entry = archive.GetEntry(DownloadEntry) ?? throw new InvalidDataException($"{DownloadFile} has no {DownloadEntry}");
            using var reader = new StreamReader(await entry.OpenAsync());
            while (await reader.ReadLineAsync() is string line)
            {
                var columns = line.Split('\t');
                var population = int.Parse(columns[14], CultureInfo.InvariantCulture);
                if (population < ExtractMinimumPopulation || ExcludedFeatureCodes.Contains(columns[7]))
                {
                    continue;
                }

                var name = string.IsNullOrWhiteSpace(columns[2]) ? columns[1] : columns[2];
                rows.Add((population, long.Parse(columns[0], CultureInfo.InvariantCulture), string.Join('\t', columns[0], name, columns[4], columns[5], columns[8], columns[14])));
            }
        }

        string[] header =
        [
            $"# Cities of {ExtractMinimumPopulation} or more from GeoNames {DownloadEntry} ({DownloadUrl}), downloaded {DateTime.Now:yyyy-MM-dd}.",
            "# GeoNames data is licensed under CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/).",
            "# Leaves out sections of populated places (feature code PPLX, suburbs) and historical, abandoned and destroyed places (PPLH, PPLQ, PPLW).",
            "# Delete this file to rebuild it from the current GeoNames download the next time ClimateExplorer.Data.Ghcnm runs.",
            "# Columns: GeoNames ID, ASCII name, latitude, longitude, country code (ISO 3166-1 alpha-2), population. Largest first.",
        ];
        var contents = header.Concat(rows.OrderByDescending(x => x.Population).ThenBy(x => x.Id).Select(x => x.Line)).ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(ExtractFile)!);
        await File.WriteAllLinesAsync(ExtractFile, contents);
        if (Directory.Exists(Path.GetDirectoryName(ProjectExtractFile)))
        {
            await File.WriteAllLinesAsync(ProjectExtractFile, contents);
        }

        logger.LogInformation($"{rows.Count} cities written to {ExtractFile}");
    }
}
