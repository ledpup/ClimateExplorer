using System;
using System.IO.Compression;
using System.Text;
using FluentFTP;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Readers.Tar;
using SharpCompress.Readers.Zip;

namespace ClimateExplorer.AcornSatTransferFunctionAnalysis
{
    public class Program
    {
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

                if (!c.Download(out byte[] outBytes, file))
                {
                    throw new Exception("Download failed.");
                }

                File.WriteAllBytes(cacheFileName, outBytes);

                return outBytes;
            }
        }

        public static void ExtractTarGzToFolder(byte[] gzTarBytes, string outputFolder)
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            using (MemoryStream ms = new MemoryStream(gzTarBytes))
            using (GZipStream gZipStream = new GZipStream(ms, CompressionMode.Decompress))
            using (TarReader reader = TarReader.Open(gZipStream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        if (!File.Exists(Path.Combine(outputFolder, reader.Entry.Key)))
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
        }

        public static void ExtractZipToFolder(byte[] zipBytes, string outputFolder)
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            using (MemoryStream ms = new MemoryStream(zipBytes))
            using (ZipReader reader = ZipReader.Open(ms))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        if (!File.Exists(Path.Combine(outputFolder, reader.Entry.Key)))
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

        public class AdjustmentRecord
        {
            public DateOnly Date { get; set; }
            public float? UnadjustedTempMax { get; set; }
            public float? AdjustedTempMax { get; set; }
        }

        public static void DownloadAndExtractData()
        {
            if (!Directory.Exists("source-data")) Directory.CreateDirectory("source-data");

            byte[] v2DailyTmaxTarGzBytes = CachingFtpHelper.Download("ftp.bom.gov.au", "/anon/home/ncc/www/change/ACORN_SAT_daily/acorn_sat_v2.2.0_daily_tmax.tar.gz");

            ExtractTarGzToFolder(v2DailyTmaxTarGzBytes, @"source-data\acorn_sat_v2.2.0_daily_tmax");

            byte[] rawDataAndSupportingInformationZipBytes = CachingFtpHelper.Download("ftp.bom.gov.au", "/anon/home/ncc/www/change/ACORN_SAT_daily/raw-data-and-supporting-information.zip");

            ExtractZipToFolder(rawDataAndSupportingInformationZipBytes, @"source-data\raw-data-and-supporting-information");
        }

        public static void AnalyseTransferFunctions(string adjustedSiteCode, string unadjustedSiteCode, DateOnly startDate, DateOnly endDateInclusive)
        {
            var unadjustedSourceData = HqNewFileParser.ParseFile(@"source-data\raw-data-and-supporting-information\raw-data\hqnew" + unadjustedSiteCode).ToDictionary(x => x.Date);

            var adjustedSourceData = AcornSatFileParser.ParseFile(@"source-data\acorn_sat_v2.2.0_daily_tmax\tmax." + adjustedSiteCode + ".daily.csv").ToDictionary(x => x.Date);

            List<AdjustmentRecord> adjustments = new List<AdjustmentRecord>();

            DateOnly d = startDate;

            while (d <= endDateInclusive)
            {
                adjustedSourceData.TryGetValue(d, out var adjusted);
                unadjustedSourceData.TryGetValue(d, out var unadjusted);

                adjustments.Add(
                    new AdjustmentRecord()
                    {
                        Date = d,
                        AdjustedTempMax = adjusted?.Reading,
                        UnadjustedTempMax = unadjusted?.TempMax
                    });

                d = d.AddDays(1);
            }

            Console.WriteLine("Writing joined-data.csv");
            File.WriteAllLines(
                "joined-data.csv",
                adjustments.Select(x =>
                    string.Join(
                        ",",
                        new string[]
                        {
                            x.Date.ToString("yyyy-MM-dd"),
                            x.UnadjustedTempMax.ToString(),
                            x.AdjustedTempMax.ToString()
                        }
                    )
                ));

            Console.WriteLine($"Inferring transfer functions for adjusted data ({adjustedSiteCode}) relative to unadjusted data ({unadjustedSiteCode}) for range {startDate.ToString("yyyy-MM-dd")} to {endDateInclusive.ToString("yyyy-MM-dd")}.");

            DateOnly mappingStartDate = startDate;
            Dictionary<string, Tuple<float, DateOnly, DateOnly>> mapping = new Dictionary<string, Tuple<float, DateOnly, DateOnly>>();

            foreach (var adjustment in adjustments)
            {
                if (adjustment.UnadjustedTempMax != null && adjustment.AdjustedTempMax != null)
                {
                    var mappingKey = BuildMappingKey(adjustment.Date, adjustment.UnadjustedTempMax.Value);

                    if (mapping.TryGetValue(mappingKey, out var mappingInfo))
                    {
                        if (mappingInfo.Item1 != adjustment.AdjustedTempMax)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.WriteLine(
                                $"Inferring that transfer function is changing - ran from {mappingStartDate.ToString("yyyy-MM-dd")} to {adjustment.Date.AddDays(-1).ToString("yyyy-MM-dd")}. Transfer function is changing because " +
                                $"on {adjustment.Date.ToString("yyyy-MM-dd")}, month + unadjusted value {mappingKey} is mapped to {adjustment.AdjustedTempMax}, " +
                                $"but that month + unadjusted value had previously been mapped to {mappingInfo.Item1} (first on {mappingInfo.Item2}, most " +
                                $"recently on {mappingInfo.Item3}).");

                            Console.ForegroundColor = ConsoleColor.Gray;

                            //DumpMapping(mapping);

                            // Clear mapping and continue
                            mapping = new Dictionary<string, Tuple<float, DateOnly, DateOnly>>();
                            mappingStartDate = adjustment.Date;
                        }

                        mapping[mappingKey] = new Tuple<float, DateOnly, DateOnly>(adjustment.AdjustedTempMax.Value, mappingInfo.Item2, adjustment.Date);
                    }
                    else
                    {
                        mapping[mappingKey] = new Tuple<float, DateOnly, DateOnly>(adjustment.AdjustedTempMax.Value, adjustment.Date, adjustment.Date);
                    }
                }
            }
        }

        public static void Main()
        {
            DownloadAndExtractData();

            // Refer to primarysites.txt to get the mapping between source and target sites
            // For site 091311, station 091049 is the source from 1910-01-01 through 1939-03-31
            // 091311 091049 19100101 19390331 091104 19390401 20040711 091311 20040712 20191231
            AnalyseTransferFunctions("091311", "091049", new DateOnly(1910, 01, 01), new DateOnly(1939, 03, 31));


            //AnalyseTransferFunctions("091311", "091104", new DateOnly(1939, 04, 01), new DateOnly(2004, 07, 11));
            //AnalyseTransferFunctions("092045", "092045", new DateOnly(1910, 01, 01), new DateOnly(2019, 12, 31));
        }

        private static void DumpMapping(Dictionary<string, Tuple<float, DateOnly, DateOnly>> mapping)
        {
            var rows = mapping.Keys.Select(x => float.Parse(x.Split('|')[1])).Distinct().OrderBy(x => x).ToArray();

            Console.WriteLine("       Jan  Feb  Mar  Apr  May  Jun  Jul  Aug  Sep  Oct  Nov  Dec  Jan  Feb  Mar  Apr  May  Jun  Jul  Aug  Sep  Oct  Nov  Dec");

            foreach (var row in rows)
            {
                Console.Write("{0,5:#0.0}", row);

                for (int i = 1; i <= 12; i++)
                {
                    string s = i.ToString("00");
                    if (mapping.TryGetValue(s + "|" + row, out var target))
                    {
                        Console.Write("{0,5:#0.0}", target.Item1);
                    }
                    else
                    {
                        Console.Write("     ");
                    }
                }

                Console.WriteLine();
            }
        }

        public static string BuildMappingKey(DateOnly date, float reading)
        {
            return date.ToString("MM") + "|" + reading;
        }
    }
}