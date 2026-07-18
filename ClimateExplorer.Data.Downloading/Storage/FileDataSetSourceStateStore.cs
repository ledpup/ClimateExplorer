namespace ClimateExplorer.Data.Downloading.Storage;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClimateExplorer.Core.Model;

public sealed class FileDataSetSourceStateStore(string stateFolder) : IDataSetSourceStateStore
{
    private readonly string stateFolder = Path.GetFullPath(stateFolder);

    public async Task<DataSetSourceState?> GetAsync(string assetKey, CancellationToken cancellationToken)
    {
        var path = GetStatePath(assetKey);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await JsonSerializer.DeserializeAsync<DataSetSourceState>(stream, cancellationToken: cancellationToken);
    }

    public async Task PutAsync(DataSetSourceState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        Directory.CreateDirectory(stateFolder);
        var path = GetStatePath(state.AssetKey);
        var temporaryPath = path + $".tmp-{Guid.NewGuid():N}";

        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, state, cancellationToken: cancellationToken);
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

    private string GetStatePath(string assetKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetKey);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(assetKey));
        return Path.Combine(stateFolder, $"{Convert.ToHexString(hash)}.json");
    }
}
