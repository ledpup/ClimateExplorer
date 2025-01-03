using ClimateExplorer.Core;
using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using System.Text;

const double mappingTolerance = .1;
var yearToCreateAdjustedRecords = DateTime.Now.Year - 1;

var dataSets = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

var bom = dataSets.Single(x => x.Id == Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E"));
var acornSat = dataSets.Single(x => x.Id == Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"));

await UpdateAcornSatRecordsFromRaw(bom, acornSat, Enums.DataType.TempMean,
    @"..\..\..\..\ClimateExplorer.SourceData\Temperature_BOM\daily_tempmean",
    @"..\..\..\..\ClimateExplorer.SourceData\Temperature\ACORN-SAT\daily_tmean");
await UpdateAcornSatRecordsFromRaw(bom, acornSat, Enums.DataType.TempMax,
    @"..\..\..\..\ClimateExplorer.SourceData\Temperature_BOM\daily_tempmax",
    @"..\..\..\..\ClimateExplorer.SourceData\Temperature\ACORN-SAT\daily_tmax");
await UpdateAcornSatRecordsFromRaw(bom, acornSat, Enums.DataType.TempMin,
    @"..\..\..\..\ClimateExplorer.SourceData\Temperature_BOM\daily_tempmin",
    @"..\..\..\..\ClimateExplorer.SourceData\Temperature\ACORN-SAT\daily_tmin");

async Task UpdateAcornSatRecordsFromRaw(DataSetDefinition bom, DataSetDefinition acornSat, Enums.DataType dataType, string unadjustedFolder, string adjustedFolder)
{
    var unadjustedFiles = Directory.GetFiles(unadjustedFolder);

    var stations = unadjustedFiles.Select(x => new DataFileFilterAndAdjustment { Id = Path.GetFileName(x).Substring(0, 6) }).ToList();

    var mdUnadjusted = bom.MeasurementDefinitions!.Single(x => x.DataType == dataType);
    var mdAdjusted = acornSat.MeasurementDefinitions!.Single(x => x.DataType == dataType);

    mdUnadjusted.FolderName = unadjustedFolder;
    mdAdjusted.FolderName = adjustedFolder;

    Console.ForegroundColor = ConsoleColor.Gray;

    foreach (var station in stations)
    // await Parallel.ForEachAsync(tempMaxAdjustedStations, async (station, token) =>
    {
        Console.WriteLine($"{dataType}: Processing station {station.Id}");

        var adjRecords = (await DataReaderFunctions.GetDataRecords(mdAdjusted, [station])).Where(x => x.Value != null).OrderBy(x => x.Date);

        if (!adjRecords.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"No adjusted {dataType} records found for station {station.Id}. This station will be skipped.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"{dataType}: Finished station {station.Id}");
            continue;
        }

        if (adjRecords.Where(x => x.Year == yearToCreateAdjustedRecords).Any())
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"The adjusted {dataType} records for station {station} already has records for the year {yearToCreateAdjustedRecords}. This station will be skipped.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"{dataType}: Finished station {station.Id}");
            continue;
        }

        var unadjRecords = (await DataReaderFunctions.GetDataRecords(mdUnadjusted, [station])).Where(x => x.Value != null).OrderBy(x => x.Date).ToDictionary(x => x.Key!, x => x);

        if (!unadjRecords.Values.Where(x => ((DateOnly)x.Date!).Year == yearToCreateAdjustedRecords).Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"No unadjusted {dataType} records found for station {station.Id} for the year {yearToCreateAdjustedRecords}. This station will be skipped.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"{dataType}: Finished station {station.Id}");
        }

        List<AdjustmentRecord> adjustments = [];

        foreach (var adjRecord in adjRecords)
        {
            if (unadjRecords.ContainsKey(adjRecord.Key!))
            {
                var unadjRecord = unadjRecords[adjRecord.Key!];
                adjustments.Add(new AdjustmentRecord(adjRecord.Date!.Value, unadjRecord!.Value!.Value, adjRecord!.Value!.Value));
            }
        }

        var previousYearAdjustments = adjustments.Where(x => x.Date.Year == yearToCreateAdjustedRecords - 1);
        var adjustmentsWereBeingMadeForThePreviousYear = previousYearAdjustments.Any(x => Math.Round(Math.Abs(x.UnadjustedValue!.Value - x.AdjustedValue!.Value), 1) > mappingTolerance);

        if (adjustmentsWereBeingMadeForThePreviousYear)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{dataType} adjustments were being made for station {station.Id} for the year {yearToCreateAdjustedRecords - 1}. Will not update the temperature record, using BOM raw temperatures, for this station. We will need to wait for the update from ACORN-SAT instead.");
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        else
        {
            Console.WriteLine($"No {dataType} adjustments were being made for station {station.Id} in ACORN-SAT for the year {yearToCreateAdjustedRecords - 1}. Will now update the temperature record for {yearToCreateAdjustedRecords}, using BOM raw temperatures.");
            var filePath = mdAdjusted.FolderName + @"\" + mdAdjusted.FileNameFormat!.Replace("[station]", station.Id);
            var fileContents = File.ReadAllText(filePath);

            FileStream? fileStream = null;
            try
            {
                fileStream = File.OpenWrite(filePath);
                await fileStream.WriteAsync(Encoding.UTF8.GetBytes(fileContents));

                var begin = new DateOnly(yearToCreateAdjustedRecords, 1, 1);
                var end = new DateOnly(yearToCreateAdjustedRecords, 12, 31);

                for (var date = begin; date <= end; date = date.AddDays(1))
                {
                    var key = date.ToString("yyyy_M_d");
                    var value = unadjRecords.ContainsKey(key)
                        ? unadjRecords[key].Value
                        : null;

                    await fileStream.WriteAsync(Encoding.UTF8.GetBytes($"{date.ToString("yyyy-MM-dd")},{value},,\r\n"));
                }
            }
            finally
            {
                fileStream?.Close();
            }
        }

        Console.WriteLine($"{dataType}: Finished station {station.Id}");
    }//);
}

static void CreateMappingTables(List<AdjustmentRecord> adjustments)
{
    Dictionary<string, MappingRecord> mapping = [];

    if (adjustments == null || adjustments.Count == 0)
    {
        return;
    }

    DateOnly mappingStartDate = adjustments.OrderBy(x => x.Date).First().Date;

    foreach (var adjustment in adjustments)
    {
        if (adjustment.UnadjustedValue != null && adjustment.AdjustedValue != null)
        {
            var mappingKey = BuildMappingKey(adjustment.Date, adjustment.UnadjustedValue.Value);

            if (mapping.TryGetValue(mappingKey, out var mappingInfo))
            {
                if (Math.Round(Math.Abs(mappingInfo.AdjustedValue - adjustment.AdjustedValue.Value), 1) > mappingTolerance)
                {
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine(
                        $"Inferring that transfer function is changing - ran from {mappingStartDate.ToString("yyyy-MM-dd")} to {adjustment.Date.AddDays(-1).ToString("yyyy-MM-dd")}. Transfer function is changing because " +
                        $"on {adjustment.Date.ToString("yyyy-MM-dd")}, month + unadjusted value {mappingKey} is mapped to {adjustment.AdjustedValue}, " +
                        $"but that month + unadjusted value had previously been mapped to {mappingInfo.AdjustedValue} (first on {mappingInfo.FirstMapping}, most " +
                        $"recently on {mappingInfo.MostRecentMapping}).");

                    Console.ForegroundColor = ConsoleColor.Gray;

                    DumpMapping(mapping);

                    // Clear mapping and continue
                    mapping = [];
                    mappingStartDate = adjustment.Date;
                }

                mapping[mappingKey] = new MappingRecord(adjustment.AdjustedValue.Value, mappingInfo.MostRecentMapping, adjustment.Date);
            }
            else
            {
                mapping[mappingKey] = new MappingRecord(adjustment.AdjustedValue.Value, adjustment.Date, adjustment.Date);
            }
        }
    }

    DumpMapping(mapping);
}

static void DumpMapping(Dictionary<string, MappingRecord> mapping)
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
                Console.Write("{0,5:#0.0}", target.AdjustedValue);
            }
            else
            {
                Console.Write("     ");
            }
        }

        Console.WriteLine();
    }
}

static string BuildMappingKey(DateOnly date, double reading)
{
    return date.ToString("MM") + "|" + reading;
}

record MappingRecord(double AdjustedValue, DateOnly FirstMapping, DateOnly MostRecentMapping);

record AdjustmentRecord(DateOnly Date, double? UnadjustedValue, double? AdjustedValue);

