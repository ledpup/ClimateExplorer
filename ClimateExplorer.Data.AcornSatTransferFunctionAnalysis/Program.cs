using ClimateExplorer.AcornSatTransferFunctionAnalysis;
using ClimateExplorer.Core;
using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;

var dataSets = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

var bom = dataSets.Single(x => x.Id == Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E"));
var acornSat = dataSets.Single(x => x.Id == Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"));

var tempMaxUnadjustedFolder = @"..\..\..\..\ClimateExplorer.SourceData\Temperature_BOM\daily_tempmax";
var tempMaxAdjustedFolder = @"..\..\..\..\ClimateExplorer.SourceData\Temperature\ACORN-SAT\daily_tmax";

var tempMaxUnadjustedFiles = Directory.GetFiles(tempMaxUnadjustedFolder);

var tempMaxAdjustedStations = tempMaxUnadjustedFiles.Select(x => new DataFileFilterAndAdjustment { Id = Path.GetFileName(x).Substring(0, 6) }).ToList();

var mdTempMaxUnadjusted = bom.MeasurementDefinitions!.Single(x => x.DataType == Enums.DataType.TempMax);
var mdTempMaxAdjusted = acornSat.MeasurementDefinitions!.Single(x => x.DataType == Enums.DataType.TempMax);

mdTempMaxUnadjusted.FolderName = tempMaxUnadjustedFolder;
mdTempMaxAdjusted.FolderName = tempMaxAdjustedFolder;

Console.ForegroundColor = ConsoleColor.Gray;

foreach (var station in tempMaxAdjustedStations)
// await Parallel.ForEachAsync(tempMaxAdjustedStations, async (station, token) =>
{
    Console.WriteLine($"Processing station {station.Id}");

    var adjRecords = (await DataReaderFunctions.GetDataRecords(mdTempMaxAdjusted, [station])).Where(x => x.Value != null).OrderBy(x => x.Date);
    var unadjRecords = (await DataReaderFunctions.GetDataRecords(mdTempMaxUnadjusted, [station])).Where(x => x.Value != null).OrderBy(x => x.Date).ToLookup(x => x.Key, x => x);

    List<AdjustmentRecord> adjustments = [];

    foreach (var adjRecord in adjRecords)
    {
        var unadjRecord = unadjRecords[adjRecord.Key].SingleOrDefault();

        if (unadjRecord != null)
        {
            adjustments.Add(
            new AdjustmentRecord()
            {
                Date = adjRecord.Date!.Value,
                AdjustedValue = adjRecord!.Value!.Value,
                UnadjustedValue = unadjRecord!.Value!.Value,
            });
        }
    }

    var mapping = CreateMappingTable(adjustments);

    Console.WriteLine($"Finished station {station.Id}");
}//);


// Refer to primarysites.txt to get the mapping between source and target sites
// For site 091311, station 091049 is the source from 1910-01-01 through 1939-03-31
// 091311 091049 19100101 19390331 091104 19390401 20040711 091311 20040712 20191231
// AnalyseTransferFunctions("091311", "091049", new DateOnly(1910, 01, 01), new DateOnly(1939, 03, 31), "acorn_sat_v2.5.0_daily_tmax", "tmax", "v2.5-raw-data-and-supporting-information");


//AnalyseTransferFunctions("091311", "091104", new DateOnly(1939, 04, 01), new DateOnly(2004, 07, 11));
//AnalyseTransferFunctions("092045", "092045", new DateOnly(1910, 01, 01), new DateOnly(2019, 12, 31));

static void AnalyseTransferFunctions(string adjustedSiteCode, string unadjustedSiteCode, DateOnly startDate, DateOnly endDateInclusive, string acornSatfileName, string dataType, string rawDataFile)
{
    List<AdjustmentRecord> adjustments = LoadDataAndCreateAdjustments(adjustedSiteCode, unadjustedSiteCode, startDate, endDateInclusive, acornSatfileName, dataType, rawDataFile);

    Console.WriteLine($"Inferring transfer functions for adjusted data ({adjustedSiteCode}) relative to unadjusted data ({unadjustedSiteCode}) for range {startDate.ToString("yyyy-MM-dd")} to {endDateInclusive.ToString("yyyy-MM-dd")}.");

    var mapping = CreateMappingTable(adjustments);
}

static Dictionary<string, Tuple<double, DateOnly, DateOnly>> CreateMappingTable(List<AdjustmentRecord> adjustments)
{
    Dictionary<string, Tuple<double, DateOnly, DateOnly>> mapping = [];

    if (adjustments == null || adjustments.Count == 0)
    {
        return mapping;
    }

    DateOnly mappingStartDate = adjustments.OrderBy(x => x.Date).First().Date;

    foreach (var adjustment in adjustments)
    {
        if (adjustment.UnadjustedValue != null && adjustment.AdjustedValue != null)
        {
            var mappingKey = BuildMappingKey(adjustment.Date, adjustment.UnadjustedValue.Value);

            if (mapping.TryGetValue(mappingKey, out var mappingInfo))
            {
                if (mappingInfo.Item1 != adjustment.AdjustedValue)
                {
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine(
                        $"Inferring that transfer function is changing - ran from {mappingStartDate.ToString("yyyy-MM-dd")} to {adjustment.Date.AddDays(-1).ToString("yyyy-MM-dd")}. Transfer function is changing because " +
                        $"on {adjustment.Date.ToString("yyyy-MM-dd")}, month + unadjusted value {mappingKey} is mapped to {adjustment.AdjustedValue}, " +
                        $"but that month + unadjusted value had previously been mapped to {mappingInfo.Item1} (first on {mappingInfo.Item2}, most " +
                        $"recently on {mappingInfo.Item3}).");

                    Console.ForegroundColor = ConsoleColor.Gray;

                    //DumpMapping(mapping);

                    // Clear mapping and continue
                    mapping = [];
                    mappingStartDate = adjustment.Date;
                }

                mapping[mappingKey] = new Tuple<double, DateOnly, DateOnly>(adjustment.AdjustedValue.Value, mappingInfo.Item2, adjustment.Date);
            }
            else
            {
                mapping[mappingKey] = new Tuple<double, DateOnly, DateOnly>(adjustment.AdjustedValue.Value, adjustment.Date, adjustment.Date);
            }
        }
    }

    return mapping;
}

#pragma warning disable CS8321 // Local function is declared but never used
static void DumpMapping(Dictionary<string, Tuple<float, DateOnly, DateOnly>> mapping)
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
#pragma warning restore CS8321 // Local function is declared but never used

static string BuildMappingKey(DateOnly date, double reading)
{
    return date.ToString("MM") + "|" + reading;
}

static List<AdjustmentRecord> LoadDataAndCreateAdjustments(string adjustedSiteCode, string unadjustedSiteCode, DateOnly startDate, DateOnly endDateInclusive, string acornSatfileName, string dataType, string rawDataFile)
{
    var unadjustedSourceData = HqNewFileParser.ParseFile($@"source-data\raw-data-and-supporting-information\{rawDataFile}\hqnew" + unadjustedSiteCode).ToDictionary(x => x.Date);

    var adjustedSourceData = AcornSatFileParser.ParseFile($@"source-data\{acornSatfileName}\{dataType}." + adjustedSiteCode + ".daily.csv").ToDictionary(x => x.Date);

    List<AdjustmentRecord> adjustments = [];

    DateOnly d = startDate;

    while (d <= endDateInclusive)
    {
        adjustedSourceData.TryGetValue(d, out var adjusted);
        unadjustedSourceData.TryGetValue(d, out var unadjusted);

        adjustments.Add(
            new AdjustmentRecord()
            {
                Date = d,
                AdjustedValue = adjusted?.Reading,
                UnadjustedValue = unadjusted?.TempMax
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
                    x.UnadjustedValue.ToString()!,
                    x.AdjustedValue.ToString()!
                }
            )
        ));
    return adjustments;
}

public class AdjustmentRecord
{
    public DateOnly Date { get; set; }
    public double? UnadjustedValue { get; set; }
    public double? AdjustedValue { get; set; }
}

