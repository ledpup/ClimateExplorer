using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static System.Collections.Specialized.BitVector32;

namespace ClimateExplorer.Data.Ghcnm;

public class StationFileProcessor
{
    internal static async Task<List<Station>> Transform(List<Station> stations, Dictionary<string, Country> countries, short beginBeforeOrEqualTo, short endNoLaterThan, float missingYearsThreshold, ILogger<Program> logger)
    {
        var stationFileName = "ghcnm.tavg.v4.0.1.20230817.qcf.inv";
        var stationFile = (await File.ReadAllLinesAsync(@$"SiteMetaData\{stationFileName}"));

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
            // Below is an inital filter on the station.
            if (station == null)
            {
                logger.LogError($"Station {id} that is in the station file '{stationFileName}' is not found in the data file. There is no point keeping it in the list.");
                continue;
            }
            if (!(station.FirstYear.Value.Year <= beginBeforeOrEqualTo &&
                    station.LastYear.Value.Year > endNoLaterThan))
            {
                logger.LogInformation($"Station is being filtered out because it isn't old enough. Begins: {station.FirstYear.Value.Year} Ends: {station.LastYear.Value.Year}");
                continue;
            }
            if (station.YearsOfMissingData / (float)station.Age.Value > missingYearsThreshold)
            {
                logger.LogInformation($"Station is being filtered out because it has too much missing data. Begins: {station.FirstYear.Value.Year} Ends: {station.LastYear.Value.Year} Years of missing data: {station.YearsOfMissingData}");
                continue;
            }
            logger.LogInformation($"Station {id} has been accepted");

            countries.TryGetValue(countryCode, out Country? country);

            if (country == null)
            {
                logger.LogError($"Station {id} has a country code of {countryCode}. This code is not found in the country list file.");
            }

            station.CountryCode = country.Code;
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
