namespace ClimateExplorer.Data.Misc.Ozone;

using ClimateExplorer.Core.Model;
using System.Text.RegularExpressions;

public class OzoneFileReducer
{
    public static void Process(string fileName, string folderName)
    {
        var lines = File.ReadAllLines(@$"Ozone\{fileName}.csv");
        var regEx = new Regex("^(?<year>\\d{4})-(?<month>\\d{2})-(?<day>\\d{2}).*,(?<value>\\d*\\.?\\d*)$");
        var currentDay = new DateOnly(1979, 1, 1);
        var dayValues = new List<double>();

        var records = new List<DataRecord>();

        foreach (var line in lines)
        {
            var match = regEx.Match(line);

            if (match.Success)
            {
                var date = new DateOnly(int.Parse(match.Groups["year"].Value), int.Parse(match.Groups["month"].Value), int.Parse(match.Groups["day"].Value));
                if (currentDay == date)
                {
                    dayValues.Add(double.Parse(match.Groups["value"].Value));
                }
                else
                {
                    records.Add(new DataRecord(currentDay, dayValues.Average()));
                    dayValues = [];
                }

                currentDay = date;
            }
        }

        var outputLines = new List<string>
        {
            lines[0],
        };

        records.ForEach(x => outputLines.Add($"{x.Date!.Value.ToString("yyyy-MM-dd")},{x.Value!.Value:0.000}"));

        var destinationFolder = Path.Combine(Helpers.SourceDataFolder, folderName);
        var reducedFileName = $"{fileName}_reduced.csv";
        Console.WriteLine($"Writing ozone area file '{reducedFileName}' to folder '{destinationFolder}'");
        File.WriteAllLines(@$"{destinationFolder}\{reducedFileName}", outputLines);
    }
}
