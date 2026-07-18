namespace ClimateExplorer.Data.Downloading.Downloaders;

using System.Globalization;
using ClimateExplorer.Data.Downloading.Models;
using ClimateExplorer.Data.Downloading.Storage;
using ClimateExplorer.Data.Downloading.Workspace;

public sealed class GreenlandDataSetDownloader(
    GreenlandMeltDataClient client,
    DataSetSourceFileStore sourceFileStore,
    TimeProvider timeProvider) : IDataSetDownloader
{
    private const int FirstYear = 1979;

    // A provider revision to December can arrive after the new year turns over, so the
    // prior year is treated as still-volatile (re-fetched rather than reused) throughout January.
    private const int YearBoundaryGraceMonths = 1;

    // Allow for provider ingestion lag before treating a year's day count as implausibly incomplete.
    private const int CompletenessGraceDays = 10;

    private readonly GreenlandMeltDataClient client = client;
    private readonly DataSetSourceFileStore sourceFileStore = sourceFileStore;
    private readonly TimeProvider timeProvider = timeProvider;

    public string Key => "greenland-melt";

    public async Task<DataSetDownloadArtifact> DownloadAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryDirectory);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var currentYear = now.Year;

        var cachedLinesByYear = ReadPublishedLinesByYear(request.RelativePath);
        var boundaryYear = currentYear - 1;
        var refetchBoundaryYear = now.Month <= YearBoundaryGraceMonths;

        var yearsToFetch = Enumerable.Range(FirstYear, currentYear - FirstYear + 1)
            .Where(year => year == currentYear ||
                (year == boundaryYear && refetchBoundaryYear) ||
                !cachedLinesByYear.ContainsKey(year))
            .ToList();

        var linesByYear = new Dictionary<int, List<string>>(cachedLinesByYear);
        foreach (var year in yearsToFetch)
        {
            var yearData = await client.GetYearAsync(year, cancellationToken);
            var populatedDays = yearData.Values.Count(value => value.HasValue);
            ValidateYearPlausibility(year, currentYear, populatedDays, now);

            linesByYear[year] = yearData
                .Where(x => x.Value.HasValue)
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key:yyyy-MM-dd},{x.Value!.Value.ToString("0.###", CultureInfo.InvariantCulture)}")
                .ToList();
        }

        var candidatePath = DataSetDownloadPath.Resolve(temporaryDirectory, request.RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(candidatePath)!);
        var content = string.Join(
            Environment.NewLine,
            linesByYear.OrderBy(x => x.Key).SelectMany(x => x.Value)) + Environment.NewLine;
        await File.WriteAllTextAsync(candidatePath, content, cancellationToken);

        return new DataSetDownloadArtifact(candidatePath);
    }

    private static void ValidateYearPlausibility(int year, int currentYear, int populatedDays, DateTime now)
    {
        if (populatedDays == 0)
        {
            throw new InvalidDataException($"Greenland melt data for {year} contained no populated days.");
        }

        var expectedMinimumDays = year < currentYear
            ? 355
            : Math.Max(0, now.DayOfYear - CompletenessGraceDays);
        if (populatedDays < expectedMinimumDays)
        {
            throw new InvalidDataException(
                $"Greenland melt data for {year} was implausibly incomplete: {populatedDays} populated day(s), expected at least {expectedMinimumDays}.");
        }
    }

    private Dictionary<int, List<string>> ReadPublishedLinesByYear(string relativePath)
    {
        var publishedPath = sourceFileStore.ResolvePath(relativePath);
        var result = new Dictionary<int, List<string>>();
        if (!File.Exists(publishedPath))
        {
            return result;
        }

        foreach (var line in File.ReadLines(publishedPath))
        {
            if (line.Length < 4 || !int.TryParse(line.AsSpan(0, 4), out var year))
            {
                continue;
            }

            if (!result.TryGetValue(year, out var lines))
            {
                lines = [];
                result.Add(year, lines);
            }

            lines.Add(line);
        }

        return result;
    }
}
