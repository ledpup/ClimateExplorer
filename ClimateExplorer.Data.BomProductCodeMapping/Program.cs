using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

const string ReferenceFolder = "Reference";
const string StationsCsv = "acorn_sat_stations.csv";
const string StationsTxt = "acorn_sat_stations.txt";
const string FailedCodesFile = "failed_product_codes.txt";
const string MappingFile = "acorn_station_product_mapping.csv";
const string ValidResponsesFile = "valid_responses.csv";

RunAsync(args).GetAwaiter().GetResult();

async Task RunAsync(string[] args)
{
	Console.WriteLine("Starting BOM product-code → station-id mapper");

	var cwd = Directory.GetCurrentDirectory();
	var referencePath = Path.Combine(cwd, ReferenceFolder);
	if (!Directory.Exists(referencePath))
	{
		// try to find any Reference folder under the repo/workspace
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
			else
			{
				// if a single numeric or product code passed
				if (singleProductCode is null)
				{
					singleProductCode = NormalizeProductCodeOrSuffix(arg);
				}
			}
		}
	}

	// Load target station ids and names from CSV (preferred) and TXT (supplement)
	var targetStations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	if (File.Exists(csvPath))
	{
		foreach (var line in File.ReadLines(csvPath))
		{
			var trimmed = line.Trim();
			if (string.IsNullOrWhiteSpace(trimmed)) continue;
			// naive CSV: id,name,... -> id may be missing leading zero(s)
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

	Console.WriteLine($"Loaded {targetStations.Count} target stations from reference files");

	// Load previously failed codes so we don't recheck them
	var failedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	if (File.Exists(FailedCodesFile))
	{
		foreach (var line in File.ReadLines(FailedCodesFile))
		{
			var t = line.Trim(); if (t.Length == 0) continue; failedCodes.Add(t);
		}
		Console.WriteLine($"Loaded {failedCodes.Count} previously failed product codes from {FailedCodesFile}");
	}

	// Load existing mapping if present (so we don't duplicate)
	var existingMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // stationId -> productCode
	if (File.Exists(MappingFile))
	{
		foreach (var line in File.ReadLines(MappingFile))
		{
			var trimmed = line.Trim(); if (string.IsNullOrWhiteSpace(trimmed)) continue;
			var parts = trimmed.Split(',');
			if (parts.Length < 2) continue;
			var sid = NormalizeStationId(parts[0].Trim());
			var code = parts[1].Trim();
			if (!existingMappings.ContainsKey(sid)) existingMappings[sid] = code;
		}
		Console.WriteLine($"Loaded {existingMappings.Count} existing mappings from {MappingFile}");
	}

	var mappedStations = new Dictionary<string, string>(existingMappings, StringComparer.OrdinalIgnoreCase);

	using var http = new HttpClient();
	http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; BOM-Mapper/1.0)");

	var mappingWriter = new StreamWriter(new FileStream(MappingFile, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
	var validWriter = new StreamWriter(new FileStream(ValidResponsesFile, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
	var failedWriter = new StreamWriter(new FileStream(FailedCodesFile, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };

	int totalTargets = targetStations.Count;

	// If we already mapped all targets, exit
	if (mappedStations.Count >= totalTargets)
	{
		Console.WriteLine("All stations are already mapped. Exiting.");
		mappingWriter.Dispose(); validWriter.Dispose(); failedWriter.Dispose();
		return;
	}

	if (singleProductCode != null)
	{
		Console.WriteLine($"Running single-product dry-run for {singleProductCode}");
		startIndex = endIndex = int.Parse(singleProductCode.Substring(singleProductCode.Length - 4));
	}

	Console.WriteLine($"Beginning scan of {startIndex:D4}..{endIndex:D4} (will stop early if all targets mapped)");

	for (int num = startIndex; num <= endIndex; num++)
	{
		var suffix = num.ToString("D4", CultureInfo.InvariantCulture);
		var productCode = $"IDCJDW{suffix}";

		if (failedCodes.Contains(productCode)) continue; // skip known failures
		if (mappedStations.ContainsValue(productCode)) continue; // skip codes already used in mapping file

		var url = $"https://www.bom.gov.au/climate/dwo/{productCode}.latest.shtml";
		try
		{
			using var resp = await http.GetAsync(url);
			if (!resp.IsSuccessStatusCode)
			{
				failedWriter.WriteLine(productCode);
				failedCodes.Add(productCode);
				continue;
			}

			var content = await resp.Content.ReadAsStringAsync();

			// Look for station id and station name around "Source of data" section
			var stationId = ExtractStationId(content);
			var stationName = ExtractStationName(content);

			if (stationId is null)
			{
				// record this as a valid response but no station found
				validWriter.WriteLine($"{productCode},, ,{url}");
				// not a match for our targets
				failedWriter.WriteLine(productCode);
				failedCodes.Add(productCode);
				continue;
			}

			stationId = NormalizeStationId(stationId);

			// record valid response
			validWriter.WriteLine($"{productCode},{stationId},{EscapeCsv(stationName)},{url}");

			// check if it's one of our targets
			if (targetStations.TryGetValue(stationId, out var targetName))
			{
				mappedStations[stationId] = productCode;
				mappingWriter.WriteLine($"{stationId},{productCode}");

				// log match quality
				if (!string.IsNullOrEmpty(targetName) && !string.IsNullOrEmpty(stationName))
				{
					if (NamesRoughlyMatch(targetName, stationName))
						Console.WriteLine($"Perfect match: {stationId} '{targetName}' ↔ {productCode} ('{stationName}')");
					else
						Console.WriteLine($"Likely match: {stationId} '{targetName}' ↔ {productCode} ('{stationName}')");
				}
				else
				{
					Console.WriteLine($"Mapped {stationId} -> {productCode} ('{stationName}')");
				}

				if (mappedStations.Count >= totalTargets)
				{
					Console.WriteLine("All targets mapped; finishing early.");
					break;
				}
			}
			else
			{
				// Not a target station; still useful info recorded
				Console.WriteLine($"Found station outside targets: {stationId} '{stationName}' at {productCode}");
				// mark as failed for mapping purposes (so we don't re-check)
				failedWriter.WriteLine(productCode);
				failedCodes.Add(productCode);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error processing {productCode}: {ex.Message}");
			// don't spam failed file for transient errors; skip with delay
		}

		// be gentle
		await Task.Delay(150);
	}

	mappingWriter.Dispose(); validWriter.Dispose(); failedWriter.Dispose();

	Console.WriteLine($"Done. Mapped {mappedStations.Count} stations. Mapping file: {MappingFile}");
}

static string NormalizeProductCodeOrSuffix(string input)
{
	var s = input.Trim();
	if (s.StartsWith("IDCJDW", StringComparison.OrdinalIgnoreCase)) return s.ToUpperInvariant();
	// numeric suffix
	if (int.TryParse(s, out var v)) return "IDCJDW" + v.ToString("D4");
	// maybe full like IDCJDW2801.latest.shtml
	var m = Regex.Match(s, @"IDCJDW(\d{4})", RegexOptions.IgnoreCase);
	if (m.Success) return "IDCJDW" + m.Groups[1].Value;
	throw new ArgumentException($"Cannot parse product code/suffix from '{input}'");
}

// ---------------- helper methods ----------------

static string NormalizeStationId(string raw)
{
	var s = raw.Trim();
	// remove non-digits
	s = Regex.Replace(s, "\\D", "");
	if (s.Length < 6) s = s.PadLeft(6, '0');
	return s;
}

static string[] SplitCsvLine(string line)
{
	// very small CSV splitter that handles quoted values
	var pattern = new Regex(@",(?=(?:[^"" ]*""[^"" ]*"")*(?![^"" ]*""))", RegexOptions.Compiled);
	var parts = pattern.Split(line);
	for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim().Trim('"');
	return parts;
}

static string? ExtractStationId(string html)
{
	if (string.IsNullOrEmpty(html)) return null;
	var lower = html.ToLowerInvariant();
	var idx = lower.IndexOf("source of data", StringComparison.Ordinal);
	string snippet = html;
	if (idx >= 0)
	{
		snippet = html.Substring(idx, Math.Min(800, html.Length - idx));
	}

	// look for {station 070351} or station 070351 or station 70351
	var m = Regex.Match(snippet, "\\{?station\\s+0*(\\d{4,6})\\}?", RegexOptions.IgnoreCase);
	if (m.Success) return m.Groups[1].Value;

	// fallback: any 5-6 digit number after the word 'station'
		m = Regex.Match(snippet, @"station[^\n\r\t\<\>\d]*([0-9]{5,6})", RegexOptions.IgnoreCase);
	if (m.Success) return m.Groups[1].Value;

	return null;
}

static string ExtractStationName(string html)
{
	if (string.IsNullOrEmpty(html)) return string.Empty;
	var lower = html.ToLowerInvariant();
	var idx = lower.IndexOf("observations were drawn from", StringComparison.Ordinal);
	string snippet = html;
	if (idx >= 0)
	{
		snippet = html.Substring(idx, Math.Min(400, html.Length - idx));
	}

	var m = Regex.Match(snippet, @"Observations were drawn from\s+([^\{\n\<]+)", RegexOptions.IgnoreCase);
	if (m.Success) return HtmlToPlainText(m.Groups[1].Value).Trim();

	// fallback: find the h2 "Source of data" and then the following paragraph
		m = Regex.Match(html, @"<h2[^>]*>\s*Source of data\s*</h2>.*?<p[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
	if (m.Success) return HtmlToPlainText(m.Groups[1].Value).Trim();

	return string.Empty;
}

static string HtmlToPlainText(string s)
{
	if (string.IsNullOrEmpty(s)) return string.Empty;
	var noTags = Regex.Replace(s, @"<.*?>", " ");
	return System.Net.WebUtility.HtmlDecode(noTags).Replace("\n", " ").Replace("\r", " ").Trim();
}

static bool NamesRoughlyMatch(string a, string b)
{
	if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
	a = a.ToLowerInvariant(); b = b.ToLowerInvariant();
	if (a.Contains(b) || b.Contains(a)) return true;
	// strip words like 'airport', 'station'
	var strip = new[] { "airport", "aero", "station", "met", "observatory" };
	foreach (var w in strip)
	{
		a = a.Replace(w, ""); b = b.Replace(w, "");
	}
	a = Regex.Replace(a, "\\s+", " ").Trim();
	b = Regex.Replace(b, "\\s+", " ").Trim();
	if (a.Contains(b) || b.Contains(a)) return true;
	return false;
}

static string EscapeCsv(string s)
{
	if (s is null) return string.Empty;
	if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
		return '"' + s.Replace("\"", "\"\"") + '"';
	return s;
}

