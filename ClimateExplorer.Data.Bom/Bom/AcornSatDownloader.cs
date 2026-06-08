namespace ClimateExplorer.Data.Bom;

using FluentFTP;
using System;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;

public static class AcornSatDownloader
{
    public static void DownloadAndExtractData(string acornSatfileName, string outputFolder)
    {
        if (!Directory.Exists("source-data")) Directory.CreateDirectory("source-data");

        byte[] v2DailyTmaxTarGzBytes = CachingFtpHelper.Download("ftp.bom.gov.au", $"/anon/home/ncc/www/change/ACORN_SAT_daily/{acornSatfileName}.tar.gz");

        ExtractTarGzToFolder(v2DailyTmaxTarGzBytes, outputFolder);
    }

    public static void ExtractTarGzToFolder(byte[] gzTarBytes, string outputFolder)
    {
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        using MemoryStream ms = new(gzTarBytes);
        using GZipStream gZipStream = new(ms, CompressionMode.Decompress);
        using TarReader reader = new(gZipStream);

        TarEntry? entry;
        while ((entry = reader.GetNextEntry()) != null)
        {
            if (entry.EntryType == TarEntryType.Directory || entry.DataStream == null)
            {
                continue;
            }

            if (entry.Name == "README.txt")
            {
                continue;
            }

            Console.WriteLine(entry.Name);

            var destinationPath = GetDestinationPath(outputFolder, entry.Name);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            using var outStream = File.Create(destinationPath);
            entry.DataStream.CopyTo(outStream);
        }
    }

    public static void ExtractZipToFolder(byte[] zipBytes, string outputFolder)
    {
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        using MemoryStream ms = new (zipBytes);
        using ZipArchive archive = new(ms, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var destinationPath = GetDestinationPath(outputFolder, entry.FullName);
            if (File.Exists(destinationPath))
            {
                continue;
            }

            Console.WriteLine(entry.FullName);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }

    private static string GetDestinationPath(string outputFolder, string entryName)
    {
        var outputRoot = Path.GetFullPath(outputFolder);
        if (!outputRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            outputRoot += Path.DirectorySeparatorChar;
        }

        var destinationPath = Path.GetFullPath(Path.Combine(outputFolder, entryName));
        if (!destinationPath.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Archive entry {entryName} resolves outside the output folder.");
        }

        return destinationPath;
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
