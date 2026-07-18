namespace ClimateExplorer.Data.Downloading.Transformers;

using System.Globalization;
using ClimateExplorer.Core.DataPreparation;

public sealed class SeaLevelSourceFileTransformer : IDataSetSourceFileTransformer
{
    public async Task TransformAsync(string rawFilePath, string outputFilePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(rawFilePath, cancellationToken);
        var output = lines.Where(x => x.StartsWith('#')).ToList();
        output.Add("year,sea-level [mm]");
        var recordCount = 0;

        foreach (var line in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fields = line.Split(',');
            if (fields.Length < 2 ||
                !double.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalDate))
            {
                continue;
            }

            var values = new List<double>();
            foreach (var field in fields.Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (!double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
                {
                    throw new InvalidDataException("Sea-level source contained a malformed measurement.");
                }

                values.Add(value);
            }

            if (values.Count == 0)
            {
                continue;
            }

            var date = DateHelpers.ConvertDecimalDate(decimalDate);
            output.Add($"{date:yyyy-MM-dd},{values.Average().ToString("0.000", CultureInfo.InvariantCulture)}");
            recordCount++;
        }

        if (recordCount == 0)
        {
            throw new InvalidDataException("Sea-level source contained no usable measurements.");
        }

        await File.WriteAllLinesAsync(outputFilePath, output, cancellationToken);
    }
}
