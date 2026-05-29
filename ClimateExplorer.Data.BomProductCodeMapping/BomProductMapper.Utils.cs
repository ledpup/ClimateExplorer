using System.Text.RegularExpressions;

public static partial class BomProductMapper
{
    static string NormalizeProductCodeOrSuffix(string input)
    {
        var s = input.Trim();
        if (s.StartsWith("IDCJDW", StringComparison.OrdinalIgnoreCase)) return s.ToUpperInvariant();
        if (int.TryParse(s, out var v)) return "IDCJDW" + v.ToString("D4");
        var m = Regex.Match(s, @"IDCJDW(\d{4})", RegexOptions.IgnoreCase);
        if (m.Success) return "IDCJDW" + m.Groups[1].Value;
        throw new ArgumentException($"Cannot parse product code/suffix from '{input}'");
    }

    // ---------------- helper methods ----------------

    static string NormalizeStationId(string raw)
    {
        var s = raw.Trim();
        s = Regex.Replace(s, "\\D", "");
        if (s.Length < 6) s = s.PadLeft(6, '0');
        return s;
    }

    static string[] SplitCsvLine(string line)
    {
        var pattern = new Regex(@",(?=(?:[^"" ]*""[^"" ]*"")*(?![^"" ]*""))", RegexOptions.Compiled);
        var parts = pattern.Split(line);
        for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim().Trim('"');
        return parts;
    }

    static string EscapeCsv(string s)
    {
        if (s is null) return string.Empty;
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return '"' + s.Replace("\"", "\"\"") + '"';
        return s;
    }
}
