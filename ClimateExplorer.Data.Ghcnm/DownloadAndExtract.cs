using Microsoft.Extensions.Logging;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace ClimateExplorer.Data.Ghcnm;

internal class DownloadAndExtract
{
    internal static async Task<bool> DownloadAndExtractFile(HttpClient httpClient, string baseUrl, string version, string fileName, ILogger<Program> logger)
    {
        var zipFile = $@"Download\{fileName}";
        var extractFolder = $@"SourceData\{version}\";
        var errorFile = $@"SourceData\{version}\error.txt";

        Directory.CreateDirectory($@"Download");
        if (Directory.Exists(extractFolder))
        {
            Directory.Delete(extractFolder, recursive: true);
        }
        Directory.CreateDirectory(extractFolder);

        if (File.Exists(zipFile))
        {
            logger.LogInformation($"File {fileName} already exists on the drive. Will not attempt to download it again.");
        }
        else
        {
            var url = $"{baseUrl}{fileName}";
            logger.LogInformation($"Attempting to download file at {url}");
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var message = $"Failed to download file. Response status code: {response.StatusCode}. Reason phrase: {response.ReasonPhrase}";
                File.WriteAllText(errorFile, message);
                logger.LogError(message);
                return false;
            }

            using var fs = new FileStream(zipFile, FileMode.OpenOrCreate);
            await response.Content.CopyToAsync(fs);

            logger.LogInformation($"Download of file at {url} was successful");
        }
        
        try
        {
            await ExtractGzAndTar(zipFile, extractFolder, logger);
        }
        catch (Exception e)
        {
            var message = $"Failed to extract file. Message: {e.Message}";
            File.WriteAllText(errorFile, message);
            logger.LogError(message);
            return false;
        }
        return true;
    }

    static async Task ExtractGzAndTar(string gzArchiveName, string extractFolder, ILogger<Program> logger)
    {
        logger.LogInformation($"Extracting file {gzArchiveName} to {extractFolder}");

        Stream inStream = File.OpenRead(gzArchiveName);
        using var gzip = new GZipStream(inStream, CompressionMode.Decompress);

        using var unzippedStream = new MemoryStream();
        await gzip.CopyToAsync(unzippedStream);
        unzippedStream.Seek(0, SeekOrigin.Begin);

        using var reader = new TarReader(unzippedStream);

        while (reader.GetNextEntry() is TarEntry entry)
        {
            logger.LogInformation($"Entry name: {entry.Name}, entry type: {entry.EntryType}");
            var entryFile = Path.GetFileName(entry.Name);
            entry.ExtractToFile(destinationFileName: Path.Join(extractFolder, entryFile), overwrite: false);
        }
    }
}
