namespace ClimateExplorer.Data.Misc.OceanAcidity;

using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

public static class OceanAcidityReducer
{
    public static void Process(string fileName, string folderName)
    {
        var fileNameAndPath = @$"Output\Ocean\{fileName}.csv";

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            Delimiter = "\t",
        };

        using var reader = new StreamReader(fileNameAndPath);
        using var csv = new CsvReader(reader, config);

        // Skip the header
        for (int i = 0; i < 8; i++)
        {
            csv.Read();
        }

        var records = csv.GetRecords<HotSurfaceCo2InputRow>().ToList();

        var outputLines = new List<string>() { "Year,Month,Calculated pH at 25°C" };

        var datesAndValues = new Dictionary<string, List<decimal>>();

        foreach (var record in records)
        {
            var date = DateTime.Parse(record.Date!);
            var key = $"{date.Year},{date.Month}";

            if (record.Phcalc25c == "-999")
            {
                Console.WriteLine($"{key} has a null value entry. Will skip this record.");
                continue;
            }

            if (datesAndValues.ContainsKey(key))
            {
                Console.WriteLine($"{key} has more than one entry. We'll average the values for the month.");
                datesAndValues[key].Add(decimal.Parse(record.Phcalc25c!));
            }
            else
            {
                datesAndValues.Add(key, [decimal.Parse(record.Phcalc25c!)]);
            }
        }

        foreach (var keyValue in datesAndValues)
        {
            outputLines.Add($"{keyValue.Key},{keyValue.Value.Average()}");
        }

        var destinationFolder = Path.Combine(Helpers.SourceDataFolder, folderName);
        var reducedFileName = $"{fileName}_reduced.csv";
        Console.WriteLine($"Writing ocean acidity file '{reducedFileName}' to folder '{destinationFolder}'");
        File.WriteAllLines(@$"{destinationFolder}\{reducedFileName}", outputLines);
    }
}
