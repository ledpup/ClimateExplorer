using ClimateExplorer.Core;
using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;

var dataSets = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

var bom = dataSets.Single(x => x.Id == Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E"));

var tempMaxFolder =  @"..\..\..\..\ClimateExplorer.SourceData\Temperature\BOM\daily_tempmax";
var tempMinFolder = @"..\..\..\..\ClimateExplorer.SourceData\Temperature\BOM\daily_tempmin";
var tempMeanFolder = @"..\..\..\..\ClimateExplorer.SourceData\Temperature\BOM\daily_tempmean";

var tempMaxFiles = Directory.GetFiles(tempMaxFolder);
var tempMinFiles = Directory.GetFiles(tempMinFolder);

var tempMaxStations = tempMaxFiles.Select(x => new DataFileFilterAndAdjustment { Id = Path.GetFileName(x).Substring(0, 6) }).ToList();
var tempMinStations = tempMinFiles.Select(x => new DataFileFilterAndAdjustment { Id = Path.GetFileName(x).Substring(0, 6) }).ToList();

var mdTempMax = bom.MeasurementDefinitions!.Single(x => x.DataType == Enums.DataType.TempMax);
var mdTempMin = bom.MeasurementDefinitions!.Single(x => x.DataType == Enums.DataType.TempMin);

mdTempMax.FolderName = tempMaxFolder;
mdTempMin.FolderName = tempMinFolder;

if (Directory.Exists(tempMeanFolder))
{
    Directory.Delete(tempMeanFolder, true);
}
Directory.CreateDirectory(tempMeanFolder);

await Parallel.ForEachAsync(tempMaxStations, async (station, token) =>
{
    Console.WriteLine($"Processing station {station.Id}");

    var maxRecords = (await DataReader.GetDataRecords(mdTempMax, [station])).Where(x => x.Value != null);
    var minRecords = (await DataReader.GetDataRecords(mdTempMin, [station])).Where(x => x.Value != null);
    var meanRecords = new List<DataRecord>();

    foreach (var maxRecord in maxRecords)
    {
        // The following line is what's causing poor performance. Should fix.
        var minRecord = minRecords.FirstOrDefault(x => x.Date == maxRecord.Date);

        if (minRecord != null)
        {
            var value = Math.Round((double)(maxRecord.Value! + minRecord.Value!) / 2D, 2);
            var meanRecord = new DataRecord((DateTime)maxRecord.Date!, value);
            meanRecords.Add(meanRecord);
        }
        else
        {
            Console.WriteLine($"Min temperature record doesn't exist on {maxRecord.Date} for station {station.Id}");
        }
    }

    if (meanRecords.Count > 0)
    {
        Console.WriteLine($"Writing station {station.Id} to disk.");
        var outPutLines = new List<string>();
        meanRecords.ForEach(x => outPutLines.Add($"{x.Year}{x.Month?.ToString().PadLeft(2, '0')}{x.Day?.ToString().PadLeft(2, '0')},{x.Value}"));
        File.WriteAllLines($"{tempMeanFolder}\\{station.Id}_daily_tempmean.csv", outPutLines);
        Console.WriteLine($"Finished writing station {station.Id} to disk.");
    }
    else
    {
        Console.WriteLine($"No mean records for station {station.Id}");
    }

    Console.WriteLine($"Finished station {station.Id}");
});

