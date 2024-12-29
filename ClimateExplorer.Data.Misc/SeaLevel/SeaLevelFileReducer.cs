namespace ClimateExplorer.Data.Misc.Ozone;

using ClimateExplorer.Core.Model;
using System.Text.RegularExpressions;

public class SeaLevelFileReducer
{
    public static void Process(string fileName)
    {
        var lines = File.ReadAllLines(@$"SeaLevel\{fileName}.csv");
        var regEx = new Regex("^(?<year>\\d{4})\\.(?<decimalDays>\\d+),(?<topex>-?\\d*\\.?\\d*),(?<jason1>-?\\d*\\.?\\d*),(?<jason2>-?\\d*\\.?\\d*),(?<jason3>-?\\d*\\.?\\d*)$");
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

                if (!string.IsNullOrWhiteSpace(match.Groups["topex"].Value))
                {
                    values.Add(double.Parse(match.Groups["topex"].Value));
                }

                if (!string.IsNullOrWhiteSpace(match.Groups["jason1"].Value))
                {
                    values.Add(double.Parse(match.Groups["jason1"].Value));
                }

                if (!string.IsNullOrWhiteSpace(match.Groups["jason2"].Value))
                {
                    values.Add(double.Parse(match.Groups["jason2"].Value));
                }

                if (!string.IsNullOrWhiteSpace(match.Groups["jason3"].Value))
                {
                    values.Add(double.Parse(match.Groups["jason3"].Value));
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

        File.WriteAllLines(@$"Output\{fileName}_reduced.csv", outputLines);
    }
}
