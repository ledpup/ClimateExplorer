﻿using ClimateExplorer.Core.Model;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ClimateExplorer.Data.Bom;

public static class BomLocationsAndStationsMapper
{
    public static async Task<List<Station>> BuildAcornSatLocationsFromReferenceMetaDataAsync(Guid dataSetDefinitionId, string outputFileSuffix)
    {
        var oldLocations = await Location.GetLocationsFromFile(@"ReferenceMetaData\ACORN-SAT\Locations.json");

        var locations = new List<Location>();
        var stations = new List<Station>();
        var dataFileMapping = new DataFileMapping() { DataSetDefinitionId = dataSetDefinitionId };
        var stationToLocationMapping = new Dictionary<string, Guid>();

        // Get the friendly location name and the "primary station", as best we can do.
        var locationRowData = File.ReadAllLines(@"ReferenceMetaData\ACORN-SAT\acorn_sat_v2.3.0_stations.csv");
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
                CountryCode = "AS",
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


        var primarySitesFile = File.ReadAllLines(@"ReferenceMetaData\ACORN-SAT\primarysites.txt");
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

                var start = DateOnly.ParseExact(stationSubGroups["start"].Value, "yyyyMMdd", provider);
                var end = DateOnly.ParseExact(stationSubGroups["end"].Value, "yyyyMMdd", provider);

                // 1910/01/01 is the start-date for ACORN-SAT. We're only dealing with unadjusted data for the dates. Therefore, we can set 1910/01/01 to null and start when the data starts
                var startDate = start == DateOnly.Parse("1910/01/01") ? null : (DateOnly?)start;
                // 2019/12/31 is the end-date for ACORN-SAT 2.1, what we have for the primary sites file. We don't need to honour that date.
                var endDate = end == DateOnly.Parse("2019/12/31") ? null : (DateOnly?)end;

                var dataFileFilterAndAdjustment = new DataFileFilterAndAdjustment()
                {
                    Id = externalStationCode,
                    StartDate = startDate,
                    EndDate = endDate,
                };
                locationDataFileFilterAndAdjustments.Add(dataFileFilterAndAdjustment);

                if (!stations.Any(x => x.Id == externalStationCode))
                {
                    stations.Add(
                        new Station
                        {
                            Id = externalStationCode,
                            CountryCode = "AS"
                        });
                }
            }

            Guid? locationId = stationToLocationMapping.ContainsKey(primarySite) ? stationToLocationMapping[primarySite] : null;
            if (!locationId.HasValue)
            {
                locationDataFileFilterAndAdjustments.ForEach(x =>
                {
                    if (!locationId.HasValue)
                    {
                        locationId = stationToLocationMapping.ContainsKey(x.Id) ? stationToLocationMapping[primarySite] : null;
                    }
                });
            }

            if (!locationId.HasValue)
            {
                throw new Exception();
            }
            dataFileMapping.LocationIdToDataFileMappings.Add(locationId.Value, locationDataFileFilterAndAdjustments);
        }

        WriteFiles(outputFileSuffix, locations, stations, dataFileMapping);

        return stations;
    }

    private static void WriteFiles(string outputFileSuffix, List<Location> locations, List<Station> stations, DataFileMapping dataFileMapping)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter()
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        Directory.CreateDirectory(@"Output\Location");
        Directory.CreateDirectory(@"Output\Station");
        Directory.CreateDirectory(@"Output\DataFileMapping");

        File.WriteAllText($@"Output\Location\Locations{outputFileSuffix}.json", JsonSerializer.Serialize(locations, options));
        File.WriteAllText($@"Output\Station\Stations{outputFileSuffix}.json", JsonSerializer.Serialize(stations, options));
        File.WriteAllText($@"Output\DataFileMapping\DataFileMapping{outputFileSuffix}.json", JsonSerializer.Serialize(dataFileMapping, options));
    }

    internal static async Task BuildAcornSatAdjustedDataFileMappingAsync(Guid dataSetDefinitionId, string unadjustedDataFileMappingPath, string outputFileSuffix)
    {
        var file = await File.ReadAllTextAsync(unadjustedDataFileMappingPath);
        var unadjustedDataFileMapping = JsonSerializer.Deserialize<DataFileMapping>(file);
        var locationIdToDataFileMappings = unadjustedDataFileMapping!.LocationIdToDataFileMappings;
        var stations = await File.ReadAllLinesAsync(@"ReferenceMetaData\ACORN-SAT\acorn_sat_v2.3.0_stations.txt");

        var dataFileMapping = new DataFileMapping() { DataSetDefinitionId = dataSetDefinitionId };
        foreach (var station in stations)
        {
            foreach (var locationIdToDataFileMapping in locationIdToDataFileMappings)
            {
                if (locationIdToDataFileMapping.Value.Any(x => x.Id == station))
                {
                    if (!dataFileMapping.LocationIdToDataFileMappings.ContainsKey(locationIdToDataFileMapping.Key))
                    {
                        dataFileMapping.LocationIdToDataFileMappings.Add(locationIdToDataFileMapping.Key, new List<DataFileFilterAndAdjustment>());
                        dataFileMapping.LocationIdToDataFileMappings[locationIdToDataFileMapping.Key].Add(new DataFileFilterAndAdjustment { Id = station });
                    }
                    break;
                }
            }
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        File.WriteAllText($@"Output\DataFileMapping\DataFileMapping{outputFileSuffix}.json", JsonSerializer.Serialize(dataFileMapping, options));
    }
}
