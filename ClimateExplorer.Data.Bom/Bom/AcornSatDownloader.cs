namespace ClimateExplorer.Data.Bom;

using FluentFTP;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Readers.Zip;
using System;
using System.IO.Compression;
using System.Text;

public static class AcornSatDownloader
{
    public static void DownloadAndExtractData(string acornSatfileName, string outputFolder, Dictionary<string, string> fileRenames)
    {
        if (!Directory.Exists("source-data")) Directory.CreateDirectory("source-data");

        byte[] v2DailyTmaxTarGzBytes = CachingFtpHelper.Download("ftp.bom.gov.au", $"/anon/home/ncc/www/change/ACORN_SAT_daily/{acornSatfileName}.tar.gz");

        ExtractTarGzToFolder(v2DailyTmaxTarGzBytes, outputFolder, fileRenames);
    }

    public static void ExtractTarGzToFolder(byte[] gzTarBytes, string outputFolder, Dictionary<string, string> fileRenames)
    {
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        using MemoryStream ms = new(gzTarBytes);
        using GZipStream gZipStream = new(ms, CompressionMode.Decompress);
        using var reader = ReaderFactory.Open(gZipStream);

        while (reader.MoveToNextEntry())
        {
            if (!reader.Entry.IsDirectory)
            {
                if (reader.Entry.Key! == "README.txt")
                {
                    continue;
                }

                var fileName = fileRenames.ContainsKey(reader.Entry.Key!)
                    ? fileRenames[reader.Entry.Key!]
                    : reader.Entry.Key!;

                Console.WriteLine(fileName);

                var destinationPath = Path.Combine(outputFolder, fileName);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

                using var entryStream = reader.OpenEntryStream();
                using var outStream = File.Create(destinationPath);
                entryStream.CopyTo(outStream);
            }
        }
    }

    public static void ExtractZipToFolder(byte[] zipBytes, string outputFolder)
    {
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        using MemoryStream ms = new (zipBytes);
        using ZipReader reader = ZipReader.Open(ms);

        while (reader.MoveToNextEntry())
        {
            if (!reader.Entry.IsDirectory)
            {
                if (!File.Exists(Path.Combine(outputFolder, reader.Entry.Key!)))
                {
                    Console.WriteLine(reader.Entry.Key);

                    reader.WriteEntryToDirectory(
                        outputFolder,
                        new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                }
            }
        }
    }
}

public static class CachingFtpHelper
{
    public static byte[] Download(string host, string file)
    {
        if (!Directory.Exists("download-cache")) Directory.CreateDirectory("download-cache");

        string cacheFileName = Path.Join("download-cache", Convert.ToBase64String(Encoding.UTF8.GetBytes(host + "|" + file)));

        if (File.Exists(cacheFileName))
        {
            Console.WriteLine($"{host} {file} already present in cache at {cacheFileName}");

            return File.ReadAllBytes(cacheFileName);
        }

        Console.WriteLine($"{host} {file} not cached. Downloading.");

        FtpClient c = new(host);

        c.Connect();

        if (!c.DownloadBytes(out byte[] outBytes, file))
        {
            throw new Exception("Download failed.");
        }

        File.WriteAllBytes(cacheFileName, outBytes);

        return outBytes;
    }
}

public class FileRename
{
    public string OriginalName { get; init; } = string.Empty;
    public string NewName { get; init; } = string.Empty;
}