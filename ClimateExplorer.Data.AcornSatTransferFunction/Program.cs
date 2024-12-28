using ClimateExplorer.AcornSatTransferFunctionAnalysis;
using ClimateExplorer.Core;
using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;

var dataSets = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

var bom = dataSets.Single(x => x.Id == Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E"));

var tempMaxUnadjustedFolder = @"..\..\..\..\ClimateExplorer.SourceData\Temperature_BOM\daily_tempmax";
var tempMaxAdjustedFolder = @"..\..\..\..\ClimateExplorer.SourceData\ACORN-SAT\daily_tmax";
//var tempMinFolder = @"..\..\..\..\ClimateExplorer.SourceData\Temperature_BOM\daily_tempmin";
//var tempMeanFolder = @"..\..\..\..\ClimateExplorer.SourceData\Temperature_BOM\daily_tempmean";

var tempMaxUnadjustedFiles = Directory.GetFiles(tempMaxUnadjustedFolder);
//var tempMaxAdjustedFiles = Directory.GetFiles(tempMaxAdjustedFolder);
//var tempMinFiles = Directory.GetFiles(tempMinFolder);

//var tempMaxUnadjustedStations = tempMaxUnadjustedFiles.Select(x => new DataFileFilterAndAdjustment { Id = Path.GetFileName(x).Substring(0, 6) }).ToList();
var tempMaxAdjustedStations = tempMaxUnadjustedFiles.Select(x => new DataFileFilterAndAdjustment { Id = Path.GetFileName(x).Substring(0, 6) }).ToList();
//var tempMinStations = tempMinFiles.Select(x => new DataFileFilterAndAdjustment { Id = Path.GetFileName(x).Substring(0, 6) }).ToList();

var mdTempMaxUnadjusted = bom.MeasurementDefinitions!.Single(x => x.DataType == Enums.DataType.TempMax);
var mdTempMaxAdjusted = bom.MeasurementDefinitions!.Single(x => x.DataType == Enums.DataType.TempMax);

mdTempMaxUnadjusted.FolderName = tempMaxUnadjustedFolder;
mdTempMaxAdjusted.FolderName = tempMaxAdjustedFolder;

foreach (var station in tempMaxAdjustedStations)
// await Parallel.ForEachAsync(tempMaxAdjustedStations, async (station, token) =>
{
    Console.WriteLine($"Processing station {station.Id}");

    var adjRecords = (await DataReaderFunctions.GetDataRecords(mdTempMaxUnadjusted, [station])).Where(x => x.Value != null).OrderByDescending(x => x.Date);
    var unadjRecords = (await DataReaderFunctions.GetDataRecords(mdTempMaxAdjusted, [station])).Where(x => x.Value != null).OrderByDescending(x => x.Date);
    var meanRecords = new List<DataRecord>();

    List<AdjustmentRecord> adjustments = [];

    foreach (var adjRecord in adjRecords)
    {
        // The following line is what's causing poor performance. Should fix.
        var minRecord = unadjRecords.FirstOrDefault(x => x.Date == adjRecord.Date);

        if (minRecord != null)
        {
            adjustments.Add(
            new AdjustmentRecord()
            {
                Date = d,
                AdjustedTempMax = adjusted?.Reading,
                UnadjustedTempMax = unadjusted?.TempMax
            });
        }
        else
        {
            Console.WriteLine($"Min temperature record doesn't exist on {maxRecord.Date} for station {station.Id}");
        }
    }

    

    Console.WriteLine($"Finished station {station.Id}");
}//);














// Refer to primarysites.txt to get the mapping between source and target sites
// For site 091311, station 091049 is the source from 1910-01-01 through 1939-03-31
// 091311 091049 19100101 19390331 091104 19390401 20040711 091311 20040712 20191231
AnalyseTransferFunctions("091311", "091049", new DateOnly(1910, 01, 01), new DateOnly(1939, 03, 31), "acorn_sat_v2.5.0_daily_tmax", "tmax", "v2.5-raw-data-and-supporting-information");


//AnalyseTransferFunctions("091311", "091104", new DateOnly(1939, 04, 01), new DateOnly(2004, 07, 11));
//AnalyseTransferFunctions("092045", "092045", new DateOnly(1910, 01, 01), new DateOnly(2019, 12, 31));

static void AnalyseTransferFunctions(string adjustedSiteCode, string unadjustedSiteCode, DateOnly startDate, DateOnly endDateInclusive, string acornSatfileName, string dataType, string rawDataFile)
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
                    x.UnadjustedTempMax.ToString()!,
                    x.AdjustedTempMax.ToString()!
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

static string BuildMappingKey(DateOnly date, float reading)
{
    return date.ToString("MM") + "|" + reading;
}

public class AdjustmentRecord
{
    public DateOnly Date { get; set; }
    public float? UnadjustedTempMax { get; set; }
    public float? AdjustedTempMax { get; set; }
}

