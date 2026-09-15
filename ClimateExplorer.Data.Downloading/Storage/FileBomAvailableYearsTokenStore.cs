namespace ClimateExplorer.Data.Downloading.Storage;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClimateExplorer.Data.Downloading.Downloaders;

public sealed class FileBomAvailableYearsTokenStore(string tokenFolder) : IBomAvailableYearsTokenStore
{
    private readonly string tokenFolder = Path.GetFullPath(tokenFolder);

    public async Task<string?> GetAsync(string stationId, BomDailyObservationCode observationCode, CancellationToken cancellationToken)
    {
        var path = GetTokenPath(stationId, observationCode);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var entry = await JsonSerializer.DeserializeAsync<TokenEntry>(stream, cancellationToken: cancellationToken);
            return entry?.Token;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            // A missing/corrupt cache entry just means we fetch a fresh token, the same as a cold cache.
            return null;
        }
    }

    public async Task PutAsync(string stationId, BomDailyObservationCode observationCode, string token, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        Directory.CreateDirectory(tokenFolder);
        var path = GetTokenPath(stationId, observationCode);
        var temporaryPath = path + $".tmp-{Guid.NewGuid():N}";

        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, new TokenEntry(stationId, observationCode, token), cancellationToken: cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public Task DeleteAsync(string stationId, BomDailyObservationCode observationCode, CancellationToken cancellationToken)
    {
        var path = GetTokenPath(stationId, observationCode);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetTokenPath(string stationId, BomDailyObservationCode observationCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stationId);
        var key = $"{stationId}:{(int)observationCode}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Path.Combine(tokenFolder, $"{Convert.ToHexString(hash)}.json");
    }

    private sealed record TokenEntry(string StationId, BomDailyObservationCode ObservationCode, string Token);
}
