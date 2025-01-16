namespace ClimateExplorer.Data.Misc.Ozone;

using ClimateExplorer.Core.Model;
using System.Text.RegularExpressions;

public class SeaLevelFileReducer
{
    public static void Process(string fileName, string folderName)
    {
        var lines = File.ReadAllLines(@$"Output\Ocean\{fileName}.csv");
        var regEx = new Regex("^(?<year>\\d{4})\\.(?<decimalDays>\\d+),(?<values>.*)$");
        var currentDay = new DateOnly(1992, 1, 1);
        var values = new List<double>();

        var records = new List<DataRecord>();

        var headerLines = new List<string>();

        foreach (var line in lines)
        {
            var match = regEx.Match(line);

            if (match.Success)
            {
                var decimalDate = double.Parse($"{match.Groups["year"].Value}.{match.Groups["decimalDays"].Value}");

                var date = Core.DataPreparation.DateHelpers.ConvertDecimalDate(decimalDate);

                if (!string.IsNullOrWhiteSpace(match.Groups["values"].Value))
                {
                    var stringValues = match.Groups["values"].Value.Split(',');

                    stringValues
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList()
                        .ForEach(x => values.Add(double.Parse(x)));
                }

                if (values.Any())
                {
                    records.Add(new DataRecord(date, values.Average()));
                }

                values = [];
            }
            else if (line.StartsWith("#"))
            {
                headerLines.Add(line);
            }
        }

        var outputLines = new List<string>();
        outputLines.AddRange(headerLines);
        outputLines.Add("year,sea-level [mm]");

        records.ForEach(x => outputLines.Add($"{x.Date!.Value.ToString("yyyy-MM-dd")},{x.Value!.Value:0.000}"));

        var destinationFolder = Path.Combine(Helpers.SourceDataFolder, folderName);
        var reducedFileName = $"{fileName}_reduced.csv";
        Console.WriteLine($"Writing sea level file '{reducedFileName}' to folder '{destinationFolder}'");
        File.WriteAllLines(@$"{destinationFolder}\{reducedFileName}", outputLines);
    }
}
