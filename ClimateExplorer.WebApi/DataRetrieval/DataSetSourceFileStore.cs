namespace ClimateExplorer.WebApi.DataRetrieval;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

internal sealed class DataSetSourceFileStore(string datasetsRoot)
{
    private readonly string datasetsRoot = Path.GetFullPath(datasetsRoot);

    public async Task PublishAsync(string candidateFilePath, string destinationRelativePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateFilePath);
        if (!File.Exists(candidateFilePath))
        {
            throw new FileNotFoundException("The validated candidate source file was not found.", candidateFilePath);
        }

        var destinationPath = ResolvePath(destinationRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        var siblingTemporaryPath = destinationPath + $".tmp-{Guid.NewGuid():N}";

        try
        {
            await using (var input = new FileStream(candidateFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var output = new FileStream(siblingTemporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await input.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
            }

            File.Move(siblingTemporaryPath, destinationPath, true);
        }
        finally
        {
            if (File.Exists(siblingTemporaryPath))
            {
                File.Delete(siblingTemporaryPath);
            }
        }
    }

    public async Task<SourceFileInfo> GetFileInfoAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = ResolvePath(relativePath);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return new SourceFileInfo(stream.Length, Convert.ToHexString(hash));
    }

    private string ResolvePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException($"Dataset source path '{relativePath}' must be relative.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(datasetsRoot, relativePath));
        if (!fullPath.StartsWith(datasetsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Dataset source path '{relativePath}' resolves outside the datasets folder.");
        }

        return fullPath;
    }

    internal sealed record SourceFileInfo(long Length, string Sha256);
}
