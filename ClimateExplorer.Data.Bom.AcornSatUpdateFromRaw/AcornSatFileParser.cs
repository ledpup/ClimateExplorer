namespace ClimateExplorer.Data.Bom.AcornSatTransferFunctionAnalysis;

public static class AcornSatFileParser
{
    public class AcornSatFileEntry
    {
        public DateOnly Date { get; set; }
        public float? Reading { get; set; }

        public override string ToString()
        {
            return $"{Date.ToString("yyyy-MM-dd")},{Reading}";
        }
    }

    public static DateOnly ParseAcornSatFileDate(string fileDate)
    {
        return new DateOnly(
            int.Parse(fileDate.Substring(0, 4)),
            int.Parse(fileDate.Substring(5, 2)),
            int.Parse(fileDate.Substring(8, 2)));
    }

    public static float? ParseHqFileReading(string fileReading)
    {
        if (fileReading == "") return null;

        return float.Parse(fileReading);
    }

    public static IEnumerable<AcornSatFileEntry> ParseFile(string path)
    {
        var lines = File.ReadAllLines(path);

        foreach (var line in lines.Skip(2))
        {
            var segments = line.Split(',');

            yield return
                new AcornSatFileEntry()
                {
                    Date = ParseAcornSatFileDate(segments[0]),
                    Reading = ParseHqFileReading(segments[1])
                };
        }
    }
}
