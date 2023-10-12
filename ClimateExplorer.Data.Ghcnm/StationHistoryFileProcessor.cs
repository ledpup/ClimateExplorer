using ClimateExplorer.Core.Model;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ClimateExplorer.Data.Ghcnm;

public class StationFile
{
    internal static async Task<List<Station>> Load(string version, List<Station> stations, Dictionary<string, Country> countries, ILogger<Program> logger)
    {
        var dir = new DirectoryInfo(@$"SourceData\{version}\");
        var stationFileName = dir.GetFiles("*.inv").Single().FullName;

        var stationFile = await File.ReadAllLinesAsync(stationFileName);

        var stationResults = new List<Station>();

        var regEx = new Regex(@"^(?<id>[\w|-]{11})\s+(?<lat>-?\d*\.\d*)\s*(?<lng>-?\d*\.\d*)\s*(?<ele>-?\d*\.\d*)\s*(?<name>[\w|\'|\/]*)\s*\*?$");

        foreach (var stationRow in stationFile)
        {
            if (!regEx.IsMatch(stationRow))
            {
                logger.LogError($"RegEx does not match '{stationRow}'. Going to drop the station");
                continue;
            }

            var groups = regEx.Match(stationRow).Groups;

            var id = groups["id"].Value;
            var stationName = groups["name"].Value;
            var countryCode = groups["id"].Value.Substring(0, 2);
            var lat = groups["lat"].Value;
            var lng = groups["lng"].Value;
            var elevation = groups["ele"].Value;

            var station = stations.SingleOrDefault(x => x.Id == id);

            if (station == null)
            {
                logger.LogError($"Station {id} that is in the station file '{stationFileName}' is not found in the data file. There is no point keeping it in the list.");
                continue;
            }

            countries.TryGetValue(countryCode, out Country? country);

            if (country == null)
            {
                logger.LogError($"Station {id} has a country code of {countryCode}. This code is not found in the country list file. Will skip this station.");
                continue;
            }

            station.CountryCode = country!.Code;
            station.Name = stationName;

            if (!string.IsNullOrWhiteSpace(lat) && !string.IsNullOrWhiteSpace(lng))
            {
                station.Coordinates = new Coordinates
                {
                    Latitude = Convert.ToSingle(lat),
                    Longitude = Convert.ToSingle(lng),
                    Elevation = string.IsNullOrWhiteSpace(elevation) ? null : Convert.ToSingle(elevation)
                };
            }

            stationResults.Add(station);
        }

        return stationResults;
    }
}
