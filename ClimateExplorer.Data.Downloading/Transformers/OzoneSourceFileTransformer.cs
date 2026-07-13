namespace ClimateExplorer.Data.Downloading.Transformers;

using System.Globalization;

public sealed class OzoneSourceFileTransformer : IDataSetSourceFileTransformer
{
    public async Task TransformAsync(string rawFilePath, string outputFilePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(rawFilePath, cancellationToken);
        if (lines.Length < 2 || !lines[0].StartsWith("datetime,", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Ozone source did not contain its expected header.");
        }

        var valuesByDay = new SortedDictionary<DateOnly, List<double>>();
        foreach (var line in lines.Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fields = line.Split(',');
            if (fields.Length != 2 ||
                !DateTime.TryParse(fields[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime) ||
                !double.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                !double.IsFinite(value))
            {
                throw new InvalidDataException("Ozone source contained a malformed observation.");
            }

            var date = DateOnly.FromDateTime(dateTime);
            if (!valuesByDay.TryGetValue(date, out var values))
            {
                values = [];
                valuesByDay.Add(date, values);
            }

            values.Add(value);
        }

        if (valuesByDay.Count == 0)
        {
            throw new InvalidDataException("Ozone source contained no usable measurements.");
        }

        var output = new List<string> { lines[0] };
        output.AddRange(valuesByDay.Select(x => $"{x.Key:yyyy-MM-dd},{x.Value.Average().ToString("0.000", CultureInfo.InvariantCulture)}"));
        await File.WriteAllLinesAsync(outputFilePath, output, cancellationToken);
    }
}
