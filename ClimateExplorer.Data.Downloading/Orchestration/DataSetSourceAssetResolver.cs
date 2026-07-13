namespace ClimateExplorer.Data.Downloading.Orchestration;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Models;

public sealed class DataSetSourceAssetResolver(string? dataFileMappingFolder = null)
{
    private readonly string? dataFileMappingFolder = dataFileMappingFolder;

    public async Task<IReadOnlyList<DataSetDownloadRequest>> ResolveAsync(
        PostDataSetsRequestBody request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var definitions = await DataSetDefinition.GetDataSetDefinitions(dataFileMappingFolder);
        var inputs = new List<ResolutionInput>();

        foreach (var specification in request.SeriesSpecifications ?? [])
        {
            var dataSet = definitions.Single(x => x.Id == specification.DataSetDefinitionId);
            var measurement = dataSet.MeasurementDefinitions!.Single(x =>
                x.DataType == specification.DataType &&
                x.DataAdjustment == specification.DataAdjustment);
            var downloaderKey = measurement.DataDownloaderKey ?? dataSet.DataDownloaderKey;
            if (downloaderKey == null)
            {
                continue;
            }

            if (dataSet.DataLocationMapping?.LocationIdToDataFileMappings.TryGetValue(specification.LocationId, out var mappedFilters) != true)
            {
                throw new InvalidOperationException($"Dataset '{dataSet.Id}' has no file mapping for location '{specification.LocationId}'.");
            }

            var filters = mappedFilters!.Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToList();
            inputs.AddRange(filters.Select(filter => new ResolutionInput(dataSet, measurement, downloaderKey, filter)));
        }

        var requestedAssets = BuildRequests(inputs);
        if (requestedAssets.Count == 0)
        {
            return requestedAssets;
        }

        var requestedAssetKeys = requestedAssets.Select(x => x.AssetKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allContributions = GetAllResolutionInputs(definitions)
            .Where(x => requestedAssetKeys.Contains(GetAssetKey(x)));
        return BuildRequests(allContributions);
    }

    public async Task<IReadOnlyList<DataSetDownloadRequest>> ResolveAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var definitions = await DataSetDefinition.GetDataSetDefinitions(dataFileMappingFolder);
        return BuildRequests(GetAllResolutionInputs(definitions));
    }

    private static IReadOnlyList<DataSetDownloadRequest> BuildRequests(IEnumerable<ResolutionInput> inputs)
    {
        var builders = new Dictionary<string, RequestBuilder>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in inputs)
        {
            var relativePath = ResolveTemplate(input.Measurement.DataFileSource.FilePathFormat, input.Filter.Id, "source path");
            var assetKey = GetAssetKey(relativePath);
            var downloadUrlTemplate = input.Measurement.DataDownloadUrl ?? input.DataSet.DataDownloadUrl;
            var downloadUrl = downloadUrlTemplate == null
                ? null
                : ResolveTemplate(downloadUrlTemplate, input.Filter.Id, "download URL");

            if (downloadUrl != null &&
                (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
            {
                throw new InvalidOperationException($"Dataset '{input.DataSet.Id}' has an invalid download URL.");
            }

            if (!builders.TryGetValue(assetKey, out var builder))
            {
                builder = new RequestBuilder(input.DataSet, input.DownloaderKey, assetKey, relativePath, downloadUrl);
                builders.Add(assetKey, builder);
            }
            else if (!string.Equals(builder.DownloaderKey, input.DownloaderKey, StringComparison.Ordinal) ||
                     !string.Equals(builder.DownloadUrl, downloadUrl, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Source asset '{assetKey}' resolves to conflicting downloader metadata.");
            }

            if (!builder.Measurements.Any(x =>
                    ReferenceEquals(x.MeasurementDefinition, input.Measurement) &&
                    string.Equals(x.FileFilter.Id, input.Filter.Id, StringComparison.Ordinal)))
            {
                builder.Measurements.Add(new DataSetDownloadMeasurement(
                    input.Measurement,
                    new DataFileFilterAndAdjustment { Id = input.Filter.Id }));
            }
        }

        return builders.Values
            .Select(x => new DataSetDownloadRequest(
                x.DataSetDefinition,
                x.DownloaderKey,
                x.AssetKey,
                x.RelativePath,
                x.DownloadUrl,
                x.Measurements))
            .ToList();
    }

    private static IEnumerable<ResolutionInput> GetAllResolutionInputs(IEnumerable<DataSetDefinition> definitions)
    {
        foreach (var dataSet in definitions)
        {
            foreach (var measurement in dataSet.MeasurementDefinitions ?? [])
            {
                var downloaderKey = measurement.DataDownloaderKey ?? dataSet.DataDownloaderKey;
                if (downloaderKey == null)
                {
                    continue;
                }

                var filters = dataSet.DataLocationMapping?.LocationIdToDataFileMappings.Values
                    .SelectMany(x => x)
                    .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                    .DistinctBy(x => x.Id)
                    .ToList();
                if (filters == null || filters.Count == 0)
                {
                    throw new InvalidOperationException($"Automatically managed dataset '{dataSet.Id}' has no file mappings.");
                }

                foreach (var filter in filters)
                {
                    yield return new ResolutionInput(dataSet, measurement, downloaderKey, filter);
                }
            }
        }
    }

    private static string GetAssetKey(ResolutionInput input)
    {
        var relativePath = ResolveTemplate(input.Measurement.DataFileSource.FilePathFormat, input.Filter.Id, "source path");
        return GetAssetKey(relativePath);
    }

    private static string GetAssetKey(string relativePath)
    {
        return relativePath.Replace('\\', '/').ToUpperInvariant();
    }

    private static string ResolveTemplate(string template, string mappedId, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        var resolved = template.Replace("[station]", mappedId, StringComparison.Ordinal);
        if (resolved.Contains('[') || resolved.Contains(']'))
        {
            throw new InvalidOperationException($"Dataset {description} '{template}' contains an unresolved placeholder.");
        }

        return resolved;
    }

    private sealed record ResolutionInput(
        DataSetDefinition DataSet,
        MeasurementDefinition Measurement,
        string DownloaderKey,
        DataFileFilterAndAdjustment Filter);

    private sealed class RequestBuilder(
        DataSetDefinition dataSetDefinition,
        string downloaderKey,
        string assetKey,
        string relativePath,
        string? downloadUrl)
    {
        public DataSetDefinition DataSetDefinition { get; } = dataSetDefinition;

        public string DownloaderKey { get; } = downloaderKey;

        public string AssetKey { get; } = assetKey;

        public string RelativePath { get; } = relativePath;

        public string? DownloadUrl { get; } = downloadUrl;

        public List<DataSetDownloadMeasurement> Measurements { get; } = [];
    }
}
