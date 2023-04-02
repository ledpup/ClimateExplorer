using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ConsoleApp1.DataRecordFileProcessor;

namespace ConsoleApp1;

public class DataRecordFileSaver
{
    public static void Save(string station, Dictionary<DateOnly, List<TimedRecord>> dataRecords)
    {
        Directory.CreateDirectory(@"Output\TempMin\");
        Directory.CreateDirectory(@"Output\TempMax\");
        Directory.CreateDirectory(@"Output\TempDewPoint\");
        Directory.CreateDirectory(@"Output\Rainfall\");

        var mins = new StreamWriter($@"Output\TempMin\{station}.csv");
        var maxs = new StreamWriter($@"Output\TempMax\{station}.csv");
        var dewPoints = new StreamWriter($@"Output\TempDewPoint\{station}.csv");

        var dewPointsAllNull = true;

        foreach (var dateOfRecord in dataRecords.Keys)
        {
            var min = dataRecords[dateOfRecord].Min(x => x.DataRecords.Where(y => y.Type == DataType.Temperature).Select(y => y.Value).Single());
            var max = dataRecords[dateOfRecord].Max(x => x.DataRecords.Where(y => y.Type == DataType.Temperature).Select(y => y.Value).Single());
            var dewPoint = dataRecords[dateOfRecord].Average(x => x.DataRecords.Where(y => y.Type == DataType.DewPointTemperature).Select(y => y.Value).Single());

            var minString = min.HasValue ? min.Value.ToString() : "null";
            var maxString = max.HasValue ? max.Value.ToString() : "null";
            var dewPointString = dewPoint.HasValue ? dewPoint.Value.ToString() : "null";

            if (dewPoint.HasValue)
            {
                dewPointsAllNull = false;
            }

            mins.WriteLine($"{dateOfRecord:yyyyMMdd},{minString}");
            maxs.WriteLine($"{dateOfRecord:yyyyMMdd},{maxString}");
            dewPoints.WriteLine($"{dateOfRecord:yyyyMMdd},{dewPointString}");
        }

        mins.Close();
        maxs.Close();
        dewPoints.Close();

        if (dewPointsAllNull)
        {
            File.Delete($@"Output\DewPoint\{station}.csv");
        }
    }
}
