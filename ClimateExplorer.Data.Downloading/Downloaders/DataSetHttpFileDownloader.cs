namespace ClimateExplorer.Data.Downloading.Downloaders;

using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

public sealed class DataSetHttpFileDownloader(HttpClient httpClient, ILogger<DataSetHttpFileDownloader>? logger = null)
{
    private const long MaximumDownloadBytes = 100 * 1024 * 1024;

    // Mid-transfer connection drops (e.g. "response ended prematurely") are common on large (~40MB) downloads
    // over flaky links, and otherwise abort the whole batch run after minutes of silent, invisible progress.
    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(15);

    // HttpClient.Timeout only bounds the time to receive headers when using HttpCompletionOption.ResponseHeadersRead
    // - it does NOT cover the subsequent body read, which is where a stalled/crawling transfer actually hangs.
    // This enforces a per-attempt ceiling on the whole download (headers + body) so a dead connection is retried
    // instead of silently sitting for however long it takes the OS/server to notice and drop it (observed: ~10
    // minutes for a single failed attempt with no timeout in place).
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromMinutes(5);

    private readonly HttpClient httpClient = httpClient;
    private readonly ILogger<DataSetHttpFileDownloader>? logger = logger;

    public async Task DownloadAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            logger?.LogInformation("Downloading {Url} (attempt {Attempt}/{MaxAttempts})", url, attempt, MaxAttempts);
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(AttemptTimeout);
            try
            {
                var totalBytes = await DownloadAttemptAsync(url, destinationPath, attemptCts.Token);
                logger?.LogInformation("Downloaded {Url}: {TotalBytes} bytes", url, totalBytes);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxAttempts && (ex is IOException or OperationCanceledException))
            {
                // A dropped connection mid-transfer (IOException, e.g. HttpIOException "response ended prematurely")
                // or a stalled one (OperationCanceledException from AttemptTimeout above) is transient - retry the
                // whole download rather than failing the entire dataset refresh.
                logger?.LogWarning(ex, "Download of {Url} failed or stalled on attempt {Attempt}/{MaxAttempts}; retrying in {RetryDelay}", url, attempt, MaxAttempts, RetryDelay);
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }

    private async Task<long> DownloadAttemptAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        RejectOversizedContent(response.Content.Headers);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        long totalBytes = 0;
        await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
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
        return totalBytes;
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
