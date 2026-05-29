using System.Globalization;
using System.Net.Http;

public static partial class BomProductMapper
{
    static async System.Threading.Tasks.Task ProcessRange(int startIndex, int endIndex, HttpClient http,
        StreamWriter mappingWriter, StreamWriter validWriter, StreamWriter failedWriter,
        Dictionary<string, string> targetStations, HashSet<string> failedCodes,
        Dictionary<string, string> mappedStations, bool progressMode)
    {
        for (int num = startIndex; num <= endIndex; num++)
        {
            var suffix = num.ToString("D4", CultureInfo.InvariantCulture);
            var productCode = $"IDCJDW{suffix}";

            if (failedCodes.Contains(productCode)) continue; // skip known failures
            if (mappedStations.ContainsValue(productCode)) continue; // skip codes already used in mapping file

            var url = $"https://www.bom.gov.au/climate/dwo/{productCode}.latest.shtml";
            try
            {
#pragma warning disable CS8602
                int retries = 0;
                HttpResponseMessage? resp = null;
                while (retries < 3)
                {
                    resp = await http.GetAsync(url);
                    if (resp.StatusCode != System.Net.HttpStatusCode.Forbidden) break; // 403 Forbidden, retry with backoff
                    retries++;
                    if (retries < 3)
                    {
                        var waitMs = 1000 * (int)Math.Pow(2, retries); // 2s, 4s
                        Console.WriteLine($"Got 403 for {productCode}; retrying in {waitMs}ms...");
                        await System.Threading.Tasks.Task.Delay(waitMs);
                    }
                }
                using (resp)
                {
                    if (resp?.IsSuccessStatusCode == false)
                    {
                        if (resp.StatusCode != System.Net.HttpStatusCode.Forbidden)
                        {
                            failedWriter.WriteLine(productCode);
                            failedCodes.Add(productCode);
                        }
                        else if (progressMode)
                        {
                            Console.WriteLine($"Skipped {productCode} (403 after retries)");
                        }
                        continue;
                    }

                    var content = await resp.Content.ReadAsStringAsync();

                    var stationId = ExtractStationId(content);
                    var stationName = ExtractStationName(content);

                    if (stationId is null)
                    {
                        validWriter.WriteLine($"{productCode},, ,{url}");
                        failedWriter.WriteLine(productCode);
                        failedCodes.Add(productCode);
                        Console.WriteLine($"Failed code: {productCode}");
                        continue;
                    }

                    stationId = NormalizeStationId(stationId);
                    validWriter.WriteLine($"{productCode},{stationId},{EscapeCsv(stationName)},{url}");

                    if (targetStations.TryGetValue(stationId, out var targetName))
                    {
                        mappedStations[stationId] = productCode;
                        mappingWriter.WriteLine($"{stationId},{productCode}");

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

                        if (mappedStations.Count >= targetStations.Count)
                        {
                            Console.WriteLine("All targets mapped; finishing early.");
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Found station outside targets: {stationId} '{stationName}' at {productCode}");
                        failedWriter.WriteLine(productCode);
                        failedCodes.Add(productCode);
                    }
                }
#pragma warning restore CS8602
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {productCode}: {ex.Message}");
            }

            if (progressMode && num % 100 == 0)
            {
                var total = endIndex - startIndex + 1;
                var processed = num - startIndex + 1;
                var percent = total > 0 ? (processed * 100.0 / total) : 0;
                Console.WriteLine($"Progress: {num:D4} of {endIndex:D4} ({percent:F1}%), mapped={mappedStations.Count}, failed-cached={failedCodes.Count}");
            }

            await System.Threading.Tasks.Task.Delay(2000 + new Random().Next(2000));
        }
    }
}
