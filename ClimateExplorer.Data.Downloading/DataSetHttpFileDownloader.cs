namespace ClimateExplorer.Data.Downloading;

using System.Net.Http.Headers;

public sealed class DataSetHttpFileDownloader(HttpClient httpClient)
{
    private const long MaximumDownloadBytes = 100 * 1024 * 1024;
    private readonly HttpClient httpClient = httpClient;

    public async Task DownloadAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        RejectOversizedContent(response.Content.Headers);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        long totalBytes = 0;
        await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var output = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            var buffer = new byte[81920];
            while (true)
            {
                var bytesRead = await input.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                totalBytes += bytesRead;
                if (totalBytes > MaximumDownloadBytes)
                {
                    throw new InvalidDataException($"Dataset download exceeded the {MaximumDownloadBytes} byte limit.");
                }

                await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            await output.FlushAsync(cancellationToken);
        }

        if (totalBytes == 0)
        {
            throw new InvalidDataException("Dataset download was empty.");
        }

        await RejectHtmlAsync(destinationPath, cancellationToken);
    }

    private static void RejectOversizedContent(HttpContentHeaders headers)
    {
        if (headers.ContentLength > MaximumDownloadBytes)
        {
            throw new InvalidDataException($"Dataset download exceeded the {MaximumDownloadBytes} byte limit.");
        }
    }

    private static async Task RejectHtmlAsync(string path, CancellationToken cancellationToken)
    {
        var buffer = new char[512];
        using var reader = new StreamReader(path);
        var length = await reader.ReadAsync(buffer, cancellationToken);
        var prefix = new string(buffer, 0, length).TrimStart();
        if (prefix.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
            prefix.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Dataset download returned HTML instead of data.");
        }
    }
}
