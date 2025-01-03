namespace ClimateExplorer.Data.Bom.AcornSatTransferFunctionAnalysis;

public static class HqNewFileParser
{
    public class HqFileEntry
    {
        public DateOnly Date { get; set; }
        public float? TempMax { get; set; }
        public float? TempMin { get; set; }

        public override string ToString()
        {
            return $"{Date.ToString("yyyy-MM-dd")},{TempMax},{TempMin}";
        }
    }

    public static DateOnly ParseHqFileDate(string fileDate)
    {
        return new DateOnly(
            int.Parse(fileDate.Substring(0, 4)),
            int.Parse(fileDate.Substring(4, 2)),
            int.Parse(fileDate.Substring(6, 2)));
    }

    public static float? ParseHqFileTemp(string fileTemp)
    {
        if (fileTemp == "-999") return null;

        return int.Parse(fileTemp) / 10.0f;
    }

    public static IEnumerable<HqFileEntry> ParseFile(string path)
    {
        var lines = File.ReadAllLines(path);

        foreach (var line in lines)
        {
            yield return
                new HqFileEntry()
                {
                    Date = ParseHqFileDate(line.Substring(6, 8)),
                    TempMax = ParseHqFileTemp(line.Substring(16, 4)),
                    TempMin = ParseHqFileTemp(line.Substring(21, 4)),
                };
        }
    }
}
