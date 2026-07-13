namespace ClimateExplorer.Data.Downloading.Transformers;

using System.Globalization;

public sealed class OceanAciditySourceFileTransformer : IDataSetSourceFileTransformer
{
    public async Task TransformAsync(string rawFilePath, string outputFilePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(rawFilePath, cancellationToken);
        var headerIndex = Array.FindIndex(lines, x => x.StartsWith("cruise\t", StringComparison.OrdinalIgnoreCase));
        if (headerIndex < 0)
        {
            throw new InvalidDataException("Ocean acidity source did not contain its expected tabular header.");
        }

        var headings = lines[headerIndex].Split('\t');
        var dateIndex = Array.FindIndex(headings, x => x.Equals("date", StringComparison.OrdinalIgnoreCase));
        var valueIndex = Array.FindIndex(headings, x => x.Equals("pHcalc_25C", StringComparison.OrdinalIgnoreCase));
        if (dateIndex < 0 || valueIndex < 0)
        {
            throw new InvalidDataException("Ocean acidity source did not contain its expected date and pH columns.");
        }

        var valuesByMonth = new SortedDictionary<(int Year, int Month), List<double>>();
        foreach (var line in lines.Skip(headerIndex + 1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fields = line.Split('\t');
            if (fields.Length <= Math.Max(dateIndex, valueIndex) || fields[valueIndex] == "-999")
            {
                continue;
            }

            if (!DateTime.TryParse(fields[dateIndex], CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date) ||
                !double.TryParse(fields[valueIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                !double.IsFinite(value))
            {
                throw new InvalidDataException("Ocean acidity source contained a malformed date or pH value.");
            }

            var key = (date.Year, date.Month);
            if (!valuesByMonth.TryGetValue(key, out var values))
            {
                values = [];
                valuesByMonth.Add(key, values);
            }

            values.Add(value);
        }

        if (valuesByMonth.Count == 0)
        {
            throw new InvalidDataException("Ocean acidity source contained no usable pH values.");
        }

        var output = new List<string> { "Year,Month,Calculated pH at 25°C" };
        output.AddRange(valuesByMonth.Select(x => $"{x.Key.Year},{x.Key.Month},{x.Value.Average().ToString("0.####", CultureInfo.InvariantCulture)}"));
        await File.WriteAllLinesAsync(outputFilePath, output, cancellationToken);
    }
}
