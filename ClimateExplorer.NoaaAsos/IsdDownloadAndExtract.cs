using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateExplorer.Data.IntegratedSurfaceData
{
    internal class IsdDownloadAndExtract
    {
        internal static async Task<bool> DownloadAndExtractFile(HttpClient httpClient, int year, string stationName, string fileName, ILogger<Program> logger)
        {
            var zipFile = $@"Output\Isd\{stationName}\{fileName}.gz";
            var extractedFile = $@"Output\Isd\{stationName}\{fileName}.txt";

            Directory.CreateDirectory($@"Output\Isd\{stationName}");

            if (File.Exists(zipFile))
            {
                logger.LogInformation($"File {fileName}.gz already exists on the drive. Will not attempt to download it again.");
            }
            else
            {
                var url = $"https://www.ncei.noaa.gov/pub/data/noaa/{year}/{fileName}.gz";
                logger.LogInformation($"Attempting to download file at {url}");
                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var message = $"Failed to download file. Response status code: {response.StatusCode}. Reason phrase: {response.ReasonPhrase}";
                    File.WriteAllText(extractedFile, message);
                    logger.LogError(message);
                    return false;
                }

                using (var fs = new FileStream(zipFile, FileMode.OpenOrCreate))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }
            
            try
            {
                ExtractGz(zipFile, extractedFile, logger);
            }
            catch (Exception e)
            {
                var message = $"Failed to extract file. Message: {e.Message}";
                File.WriteAllText(extractedFile, message);
                logger.LogError(message);
                return false;
            }
            return true;
        }

        static void ExtractGz(string gzArchiveName,  string extractedFile, ILogger<Program> logger)
        {
            logger.LogInformation($"Extracting file {gzArchiveName} to {extractedFile}");
            Stream inStream = File.OpenRead(gzArchiveName);

            using var decompressedFileStream = File.Create(extractedFile);
            using var decompressionStream = new GZipStream(inStream, CompressionMode.Decompress);
            decompressionStream.CopyTo(decompressedFileStream);
        }
    }
}
