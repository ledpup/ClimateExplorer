using System.Globalization;
using System.Net.Http;

public static partial class BomProductMapper
{
    const string ReferenceFolder = "Reference";
    const string StationsCsv = "acorn_sat_stations.csv";
    const string StationsTxt = "acorn_sat_stations.txt";
    const string FailedCodesFile = "failed_product_codes.txt";
    const string MappingFile = "acorn_station_product_mapping.csv";
    const string ValidResponsesFile = "valid_responses.csv";

    public static async System.Threading.Tasks.Task RunAsync(string[] args)
    {
        Console.WriteLine("Starting BOM product-code → station-id mapper");

        var cwd = Directory.GetCurrentDirectory();
        var referencePath = Path.Combine(cwd, ReferenceFolder);
        if (!Directory.Exists(referencePath))
        {
            var found = Directory.GetDirectories(cwd, ReferenceFolder, SearchOption.AllDirectories).FirstOrDefault();
            if (found != null) referencePath = found;
        }
        var csvPath = Path.Combine(referencePath, StationsCsv);
        var txtPath = Path.Combine(referencePath, StationsTxt);

        if (!File.Exists(csvPath) && !File.Exists(txtPath))
        {
            Console.WriteLine($"Reference files not found in {referencePath}");
            return;
        }

        // parse CLI arguments for single or range
        int startIndex = 0, endIndex = 9999;
        string? singleProductCode = null;
        var logLevel = "progress"; // default to progress
        if (args != null && args.Length > 0)
        {
            foreach (var a in args)
            {
                var arg = a.Trim();
                if (arg.StartsWith("--single=", StringComparison.OrdinalIgnoreCase) || arg.StartsWith("single=", StringComparison.OrdinalIgnoreCase))
                {
                    var val = arg.Split('=')[1];
                    singleProductCode = NormalizeProductCodeOrSuffix(val);
                }
                else if (arg.StartsWith("--range=", StringComparison.OrdinalIgnoreCase) || arg.StartsWith("range=", StringComparison.OrdinalIgnoreCase))
                {
                    var val = arg.Split('=')[1];
                    var parts = val.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[0], out var s) && int.TryParse(parts[1], out var e))
                    {
                        startIndex = Math.Clamp(s, 0, 9999);
                        endIndex = Math.Clamp(e, 0, 9999);
                    }
                }
                else if (arg.StartsWith("--start=", StringComparison.OrdinalIgnoreCase))
                {
                    var val = arg.Split('=')[1]; if (int.TryParse(val, out var s)) startIndex = Math.Clamp(s, 0, 9999);
                }
                else if (arg.StartsWith("--end=", StringComparison.OrdinalIgnoreCase))
                {
                    var val = arg.Split('=')[1]; if (int.TryParse(val, out var e)) endIndex = Math.Clamp(e, 0, 9999);
                }
                else if (arg.StartsWith("--log-level=", StringComparison.OrdinalIgnoreCase) || arg.StartsWith("log-level=", StringComparison.OrdinalIgnoreCase))
                {
                    logLevel = arg.Split('=')[1].Trim();
                }
                else
                {
                    if (singleProductCode is null)
                    {
                        singleProductCode = NormalizeProductCodeOrSuffix(arg);
                    }
                }
            }
        }

        var targetStations = LoadTargetStations(csvPath, txtPath);
        Console.WriteLine($"Loaded {targetStations.Count} target stations from reference files");

        var failedCodes = LoadFailedCodes(FailedCodesFile);
        Console.WriteLine($"Loaded {failedCodes.Count} previously failed product codes from {FailedCodesFile}");

        var existingMappings = LoadExistingMappings(MappingFile);
        Console.WriteLine($"Loaded {existingMappings.Count} existing mappings from {MappingFile}");

        var mappedStations = new Dictionary<string, string>(existingMappings, StringComparer.OrdinalIgnoreCase);

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        http.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        http.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
        http.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        http.DefaultRequestHeaders.Add("DNT", "1");
        http.DefaultRequestHeaders.Add("Connection", "keep-alive");
        http.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

        using var mappingWriter = new StreamWriter(new FileStream(MappingFile, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        using var validWriter = new StreamWriter(new FileStream(ValidResponsesFile, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        using var failedWriter = new StreamWriter(new FileStream(FailedCodesFile, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };

        int totalTargets = targetStations.Count;
        if (mappedStations.Count >= totalTargets)
        {
            Console.WriteLine("All stations are already mapped. Exiting.");
            return;
        }

        var progressMode = string.Equals(logLevel, "progress", StringComparison.OrdinalIgnoreCase);
        if (singleProductCode != null)
        {
            Console.WriteLine($"Running single-product dry-run for {singleProductCode}");
            startIndex = endIndex = int.Parse(singleProductCode.Substring(singleProductCode.Length - 4));
        }

        Console.WriteLine($"Beginning scan of {startIndex:D4}..{endIndex:D4} (will stop early if all targets mapped)");
        if (progressMode) Console.WriteLine("Progress logging enabled: status will print periodically during the scan.");

        await ProcessRange(startIndex, endIndex, http, mappingWriter, validWriter, failedWriter, targetStations, failedCodes, mappedStations, progressMode);

        Console.WriteLine($"Done. Mapped {mappedStations.Count} stations. Mapping file: {MappingFile}");
    }
}
