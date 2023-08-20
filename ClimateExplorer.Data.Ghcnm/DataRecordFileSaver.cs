using Microsoft.Extensions.Logging;

namespace ClimateExplorer.Data.Ghcnm;

public class DataRecordFileSaver
{
    internal static void Save(string station, Dictionary<DateOnly, List<TimedRecord>> dataRecords, ILogger<Program> logger)
    {
        Directory.CreateDirectory(@"Output\ProcessedRecords\");

        var outputFile = new StreamWriter($@"Output\ProcessedRecords\{station}.csv");

        foreach (var dateOfRecord in dataRecords.Keys)
        {
            var min = dataRecords[dateOfRecord].Min(x => !x.DataRecords.Any(y => y.Type == DataType.Temperature) ? null : x.DataRecords.Where(y => y.Type == DataType.Temperature).Select(y => y.Value).Single());
            var max = dataRecords[dateOfRecord].Max(x => !x.DataRecords.Any(y => y.Type == DataType.Temperature) ? null : x.DataRecords.Where(y => y.Type == DataType.Temperature).Select(y => y.Value).Single());
            var dewPoint = dataRecords[dateOfRecord].Average(x => !x.DataRecords.Any(y => y.Type == DataType.DewPointTemperature) ? null : x.DataRecords.Where(y => y.Type == DataType.DewPointTemperature).Select(y => y.Value).Single());
            float? rainfall = null;
            if (dataRecords[dateOfRecord].Any(x => x.DataRecords.Any(y => y.Type == DataType.Rainfall)))
            {
                rainfall = dataRecords[dateOfRecord].Sum(x => !x.DataRecords.Any(y => y.Type == DataType.Rainfall) ? null : x.DataRecords.Where(y => y.Type == DataType.Rainfall).Select(y => y.Value).Single());
            }

            if (min > max)
            {
                throw new Exception($"The minimum temperature ({min}) is greater than the maximum ({max}) on {dateOfRecord}.");
            }

            var minString = min.HasValue ? Math.Round(min.Value, 1).ToString() : "null";
            var maxString = max.HasValue ? Math.Round(max.Value, 1).ToString() : "null";
            var dewPointString = dewPoint.HasValue ? Math.Round(dewPoint.Value, 1).ToString() : "null";
            var rainfallString = rainfall.HasValue ? Math.Round(rainfall.Value, 1).ToString() : "null";

            outputFile.WriteLine($"{dateOfRecord:yyyyMMdd},{minString},{maxString},{dewPointString},{rainfallString}");
        }

        logger.LogInformation($"Station file {station}.csv has been saved to disk.");

        outputFile.Close();
    }
}

public class TimedRecord
{
    public TimedRecord(TimeOnly time)
    {
        Time = time;
        DataRecords = new List<DataRecord>();
    }
    public string? ReportType { get; set; }
    public TimeOnly Time { get; set; }
    public List<DataRecord> DataRecords { get; set; }
}

public class DataRecord
{
    public DataType Type { get; set; }
    public float? Value { get; set; }
}

public enum DataType
{
    Temperature,
    DewPointTemperature,
    Rainfall
}
