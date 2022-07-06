using AcornSat.Core.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AcornSat.Analyser;

public static class BomLocationsAndStationsMapper
{
    public static async Task<List<Station>> BuildAcornSatLocationsFromReferenceDataAsync(Guid dataSetDefintionId)
    {
        var oldLocations = await Location.GetLocations(@"ReferenceData\ACORN-SAT\Locations.json");

        var locations = new List<Location>();
        var stations = new List<Station>();
        var dataFileLocationMapping = new DataLocationMapping() { DataSetDefinitionId = dataSetDefintionId };
        var stationToLocationMapping = new Dictionary<string, Guid>();

        // Get the friendly location name and the "primary station", as best we can do.
        var locationRowData = File.ReadAllLines(@"ReferenceData\ACORN-SAT\acorn_sat_v2.1.0_stations.csv");
        foreach (var row in locationRowData.Skip(1))
        {
            var splitRow = row.Split(',');
            var station = splitRow[0].PadLeft(6, '0');
            var name = splitRow[1];
            var oldLocation = oldLocations.Single(x => x.Name == name);

            var location = new Location
            {
                Id = oldLocation.Id,
                Name = name,
                Coordinates = new Coordinates
                {
                    Latitude = float.Parse(splitRow[2]),
                    Longitude = float.Parse(splitRow[3]),
                    Elevation = float.Parse(splitRow[4]),
                }
            };
            locations.Add(location);

            stationToLocationMapping.Add(station, location.Id);
        }


        var primarySitesFile = File.ReadAllLines(@"ReferenceData\ACORN-SAT\primarysites.txt");
        var stationRegEx = new Regex(@"^(?<id>\d+)\s(?<start>\d+)\s(?<end>\d+)$");

        foreach (var row in primarySitesFile)
        {
            var rowRegEx = new Regex(@"^^(?<primarysite>\d+)\s(?<station1>\d+\s\d+\s\d+)\s(?<station2>\d+\s\d+\s\d+)\s(?<station3>\d+\s\d+\s\d+)$");
            var groups = rowRegEx.Match(row).Groups;
            var primarySite = groups["primarysite"].Value;

            var stationGroups = new List<string>
            {
                groups["station1"].Value,
                groups["station2"].Value,
                groups["station3"].Value
            };

            var locationDataFileFilterAndAdjustments = new List<DataFileFilterAndAdjustment>();

            var provider = CultureInfo.InvariantCulture;
            for (var i = 0; i < stationGroups.Count; i++)
            {
                var stationString = stationGroups[i];
                var stationSubGroups = stationRegEx.Match(stationString).Groups;
                var externalStationCode = stationSubGroups["id"].Value;

                if (externalStationCode == "999999")
                {
                    continue;
                }

                var start = DateTime.ParseExact(stationSubGroups["start"].Value, "yyyyMMdd", provider);
                var end = DateTime.ParseExact(stationSubGroups["end"].Value, "yyyyMMdd", provider);


                var dataFileFilterAndAdjustment = new DataFileFilterAndAdjustment() 
                { 
                    ExternalStationCode = externalStationCode,
                    StartDate = start,
                    EndDate = end,
                };
                locationDataFileFilterAndAdjustments.Add(dataFileFilterAndAdjustment);

                if (!stations.Any(x => x.ExternalStationCode == externalStationCode))
                {
                    stations.Add(new Station { ExternalStationCode = externalStationCode });
                }
            }

            Guid? locationId = stationToLocationMapping.ContainsKey(primarySite) ? stationToLocationMapping[primarySite] : null;
            if (!locationId.HasValue)
            {
                locationDataFileFilterAndAdjustments.ForEach(x =>
                {
                    if (!locationId.HasValue)
                    {
                        locationId = stationToLocationMapping.ContainsKey(x.ExternalStationCode) ? stationToLocationMapping[primarySite] : null;
                    }
                });
            }

            if (!locationId.HasValue)
            {
                throw new Exception();
            }
            dataFileLocationMapping.LocationIdToDataFileMappings.Add(locationId.Value, locationDataFileFilterAndAdjustments);
        }
       

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
        File.WriteAllText(@"Output\Locations.json", JsonSerializer.Serialize(locations, options));
        File.WriteAllText(@"Output\Stations.json", JsonSerializer.Serialize(stations, options));
        File.WriteAllText(@"Output\DataFileLocationMapping.json", JsonSerializer.Serialize(dataFileLocationMapping, options));

        return stations;
    }


    public static async Task BuildAdjustedList()
    {
        // Get the most recent list of files names for ACORN-SAT files.
        var adjustedStationV220List = File.ReadAllLines(@"ReferenceData\ACORN-SAT\acorn_sat_v2.2.0_stations.txt").ToList();

        //var location = adjustedStationV220List.Single(x => x == primarySite);

        adjustedStationV220List.ForEach(x =>
        {
            //if (!locations.Any(y => x == y.Stations.Single().ExternalStationCode))
            //{
            //    throw new Exception($"We can not match up the ACORN-SAT primary station and location for station code {x}");
            //}
        });
        return;
    }

    static async Task RestoreLocationIds(List<Location> locations, string filePath)
    {
        // Load an old locations file (from the last time we built these locations).
        // Match on the primary station
        // If we find a match, replace the new Id with the old one, so that we may main Ids between builds of this meta-data
        var oldLocations = await Location.GetLocations(filePath);

        foreach (var location in locations)
        {
            Location oldLocation = null;
            if (!string.IsNullOrWhiteSpace(location.Name))
            {
                oldLocation = oldLocations.SingleOrDefault(x => x.Name == location.Name);
            }

            if (oldLocation != null)
            {
                location.Id = oldLocation.Id;
            }
            else
            {
                throw new Exception("Unable to find a location in the restored list of locations. This should be because it is new. If this is expected, instead of this exception, assign a new GUID.");
            }
        }
    }
}
