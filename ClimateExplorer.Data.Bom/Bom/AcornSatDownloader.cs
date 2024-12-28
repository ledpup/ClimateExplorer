using FluentFTP;
using System.Text;

namespace ClimateExplorer.Data.Bom;

using System;
using System.IO.Compression;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Readers.Zip;
using SharpCompress.Readers.Tar;

public static class AcornSatDownloader
{
    public static void DownloadAndExtractData(string acornSatfileName)
    {
        if (!Directory.Exists("source-data")) Directory.CreateDirectory("source-data");

        byte[] v2DailyTmaxTarGzBytes = CachingFtpHelper.Download("ftp.bom.gov.au", $"/anon/home/ncc/www/change/ACORN_SAT_daily/{acornSatfileName}.tar.gz");

        ExtractTarGzToFolder(v2DailyTmaxTarGzBytes, $@"source-data\{acornSatfileName}");
    }

    public static void ExtractTarGzToFolder(byte[] gzTarBytes, string outputFolder)
    {
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        using MemoryStream ms = new (gzTarBytes);
        using GZipStream gZipStream = new (ms, CompressionMode.Decompress);
        using TarReader reader = TarReader.Open(gZipStream);
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
                            ExtractFullPath = true
                        });
                }
            }
        }
    }

    public static void ExtractZipToFolder(byte[] zipBytes, string outputFolder)
    {
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        using MemoryStream ms = new MemoryStream(zipBytes);
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

        FtpClient c = new FtpClient(host);

        c.Connect();

        if (!c.DownloadBytes(out byte[] outBytes, file))
        {
            throw new Exception("Download failed.");
        }

        File.WriteAllBytes(cacheFileName, outBytes);

        return outBytes;
    }
}