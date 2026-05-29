public static partial class BomProductMapper
{
    static Dictionary<string, string> LoadTargetStations(string csvPath, string txtPath)
    {
        var targetStations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(csvPath))
        {
            foreach (var line in File.ReadLines(csvPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                var parts = SplitCsvLine(trimmed);
                if (parts.Length < 2) continue;
                var idRaw = parts[0].Trim();
                var name = parts[1].Trim();
                if (string.IsNullOrEmpty(idRaw) || string.IsNullOrEmpty(name)) continue;
                var id = NormalizeStationId(idRaw);
                if (!targetStations.ContainsKey(id)) targetStations[id] = name;
            }
        }
        if (File.Exists(txtPath))
        {
            foreach (var line in File.ReadLines(txtPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                var id = NormalizeStationId(trimmed);
                if (!targetStations.ContainsKey(id)) targetStations[id] = string.Empty;
            }
        }
        return targetStations;
    }

    static HashSet<string> LoadFailedCodes(string failedFile)
    {
        var failed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(failedFile))
        {
            foreach (var line in File.ReadLines(failedFile))
            {
                var t = line.Trim(); if (t.Length == 0) continue; failed.Add(t);
            }
        }
        return failed;
    }

    static Dictionary<string, string> LoadExistingMappings(string mappingFile)
    {
        var existing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(mappingFile))
        {
            foreach (var line in File.ReadLines(mappingFile))
            {
                var trimmed = line.Trim(); if (string.IsNullOrWhiteSpace(trimmed)) continue;
                var parts = trimmed.Split(',');
                if (parts.Length < 2) continue;
                var sid = NormalizeStationId(parts[0].Trim());
                var code = parts[1].Trim();
                if (!existing.ContainsKey(sid)) existing[sid] = code;
            }
        }
        return existing;
    }
}
