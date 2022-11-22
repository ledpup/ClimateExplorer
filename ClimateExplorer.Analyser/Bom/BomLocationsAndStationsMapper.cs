using ClimateExplorer.Core.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClimateExplorer.Analyser.Bom;

public static class BomLocationsAndStationsMapper
{
    public static async Task<List<Station>> BuildAcornSatLocationsFromReferenceDataAsync(Guid dataSetDefinitionId, string outputFileSuffix)
    {
        var oldLocations = await Location.GetLocationsFromFile(@"ReferenceData\ACORN-SAT\Locations.json");

        var locations = new List<Location>();
        var stations = new List<Station>();
        var dataFileLocationMapping = new DataFileLocationMapping() { DataSetDefinitionId = dataSetDefinitionId };
        var stationToLocationMapping = new Dictionary<string, Guid>();

        // Get the friendly location name and the "primary station", as best we can do.
        var locationRowData = File.ReadAllLines(@"ReferenceData\ACORN-SAT\acorn_sat_v2.3.0_stations.csv");
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

                // 1910/01/01 is the start-date for ACORN-SAT. We're only dealing with unadjusted data for the dates. Therefore, we can set 1910/01/01 to null and start when the data starts
                var startDate = start == DateTime.Parse("1910/01/01") ? null : (DateTime?)start;
                // 2019/12/31 is the end-date for ACORN-SAT 2.1, what we have for the primary sites file. We don't need to honour that date.
                var endDate = end == DateTime.Parse("2019/12/31") ? null : (DateTime?)end;

                var dataFileFilterAndAdjustment = new DataFileFilterAndAdjustment()
                {
                    ExternalStationCode = externalStationCode,
                    StartDate = startDate,
                    EndDate = endDate,
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

        WriteFiles(outputFileSuffix, locations, stations, dataFileLocationMapping);

        return stations;
    }

    private static void WriteFiles(string outputFileSuffix, List<Location> locations, List<Station> stations, DataFileLocationMapping dataFileLocationMapping)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
        Directory.CreateDirectory(@"Output\Location");
        Directory.CreateDirectory(@"Output\Station");
        Directory.CreateDirectory(@"Output\DataFileLocationMapping");

        File.WriteAllText($@"Output\Location\Locations{outputFileSuffix}.json", JsonSerializer.Serialize(locations, options));
        File.WriteAllText($@"Output\Station\Stations{outputFileSuffix}.json", JsonSerializer.Serialize(stations, options));
        File.WriteAllText($@"Output\DataFileLocationMapping\DataFileLocationMapping{outputFileSuffix}.json", JsonSerializer.Serialize(dataFileLocationMapping, options));
    }

    internal static async Task BuildAcornSatAdjustedDataFileLocationMappingAsync(Guid dataSetDefinitionId, string unadjustedDataFileLocationMappingPath, string outputFileSuffix)
    {
        var file = await File.ReadAllTextAsync(unadjustedDataFileLocationMappingPath);
        var unadjustedDataFileLocationMapping = JsonSerializer.Deserialize<DataFileLocationMapping>(file);
        var locationIdToDataFileMappings = unadjustedDataFileLocationMapping.LocationIdToDataFileMappings;
        var stations = await File.ReadAllLinesAsync(@"ReferenceData\ACORN-SAT\acorn_sat_v2.3.0_stations.txt");

        var dataFileLocationMapping = new DataFileLocationMapping() { DataSetDefinitionId = dataSetDefinitionId };
        foreach (var station in stations)
        {
            foreach (var locationIdToDataFileMapping in locationIdToDataFileMappings)
            {
                if (locationIdToDataFileMapping.Value.Any(x => x.ExternalStationCode == station))
                {
                    if (!dataFileLocationMapping.LocationIdToDataFileMappings.ContainsKey(locationIdToDataFileMapping.Key))
                    {
                        dataFileLocationMapping.LocationIdToDataFileMappings.Add(locationIdToDataFileMapping.Key, new List<DataFileFilterAndAdjustment>());
                        dataFileLocationMapping.LocationIdToDataFileMappings[locationIdToDataFileMapping.Key].Add(new DataFileFilterAndAdjustment { ExternalStationCode = station });
                    }
                    break;
                }
            }
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
        File.WriteAllText($@"Output\DataFileLocationMapping\DataFileLocationMapping{outputFileSuffix}.json", JsonSerializer.Serialize(dataFileLocationMapping, options));
    }

    public static async Task BuildRaiaLocationsFromReferenceDataAsync(Guid dataSetDefinitionId, string outputFileSuffix)
    {
        var oldLocations = await Location.GetLocationsFromFile(@"ReferenceData\RAIA\Locations.json");

        var locations = new List<Location>();
        var stations = new List<Station>();
        var dataFileLocationMapping = new DataFileLocationMapping() { DataSetDefinitionId = dataSetDefinitionId };

        var locationRowData = File.ReadAllLines(@"ReferenceData\RAIA\RAIA.csv");
        foreach (var row in locationRowData)
        {
            var splitRow = row.Split(',');
            var stationCode = splitRow[0].PadLeft(6, '0');
            var name = splitRow[1];
            var oldLocation = oldLocations.Single(x => x.Name == name);

            var coordinates = new Coordinates
            {
                Latitude = float.Parse(splitRow[2]),
                Longitude = float.Parse(splitRow[3]),
                Elevation = float.Parse(splitRow[4]),
            };

            var location = new Location
            {
                Id = oldLocation.Id,
                Name = name,
                Coordinates = coordinates,
            };
            locations.Add(location);

            dataFileLocationMapping.LocationIdToDataFileMappings.Add(location.Id, new List<DataFileFilterAndAdjustment>());
            dataFileLocationMapping.LocationIdToDataFileMappings[location.Id].Add(new DataFileFilterAndAdjustment { ExternalStationCode = stationCode });

            stations.Add(new Station { ExternalStationCode = stationCode, Coordinates = coordinates });
        }

        WriteFiles(outputFileSuffix, locations, stations, dataFileLocationMapping);
    }
}
