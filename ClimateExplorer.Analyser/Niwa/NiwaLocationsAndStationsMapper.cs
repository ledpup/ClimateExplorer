using ClimateExplorer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClimateExplorer.Analyser.Niwa;

public static class NiwaLocationsAndStationsMapper
{
    public static async Task BuildNiwaLocationsAsync(Guid dataSetDefinitionId, string sourceLocationsFileName, string oldLocationsFileName, string outputFileSuffix)
    {
        var oldLocations = await Location.GetLocationsFromFile($@"ReferenceMetaData\NIWA\{oldLocationsFileName}");

        var locations = new List<Location>();
        var stations = new List<Station>();
        var dataFileLocationMapping = new DataFileLocationMapping() { DataSetDefinitionId = dataSetDefinitionId };

        var regEx = new Regex(@"^(?<name>[\w|\s]*),(?<station>\d+),\w\d+\w?,(?<lat>-?\d+\.\d+),(?<lng>-?\d+\.\d+),(?<alt>-?\d+),.*,.*,(?<startdate>null|\d{4}\/\d{2}\/\d{2})\s*,(?<enddate>null|\d{4}\/\d{2}\/\d{2})\s*,(?<adjustment>0|-?\d*\.\d*)\s*,(?<seriesName>[\w|'|\s|-]*),(?<stationName>[\w|\s|,]*)$");
        var locationRowData = File.ReadAllLines(@$"ReferenceMetaData\NIWA\{sourceLocationsFileName}");

        // Create the initial list of locations
        foreach (var row in locationRowData)
        {
            var match = regEx.Match(row);

            var locationName = match.Groups["name"].Value;

            var location = locations.SingleOrDefault(x => x.Name == locationName);

            var coordinates = new Coordinates
            {
                Latitude = float.Parse(match.Groups["lat"].Value),
                Longitude = float.Parse(match.Groups["lng"].Value),
                Elevation = float.Parse(match.Groups["alt"].Value),
            };

            if (location == null)
            {
                location = new Location
                {
                    Id = Guid.NewGuid(),
                    Name = locationName,
                    CountryCode = "NZ",
                    Coordinates = coordinates,
                };
                locations.Add(location);
            }

            // The last entry will have the coordinates we'll use for this location. Just update them everytime.
            location.Coordinates = coordinates;

            var oldLocation = oldLocations.SingleOrDefault(x => x.Name == location.Name);
            if (oldLocation != null)
            {
                location.Id = oldLocation.Id;
            }
            else
            {
                throw new Exception("Expecting that there would be no unknown locations");
            }

            var externalStationCode = match.Groups["station"].Value;
            var dataFileFilterAndAdjustment = new DataFileFilterAndAdjustment
            {
                Id = externalStationCode,
                StartDate = match.Groups["startdate"].Value == "null" ? null : DateTime.Parse(match.Groups["startdate"].Value),
                EndDate = match.Groups["enddate"].Value == "null" ? null : DateTime.Parse(match.Groups["enddate"].Value),
                ValueAdjustment = float.Parse(match.Groups["adjustment"].Value)
            };

            if (!stations.Any(x => x.Id == externalStationCode))
            {
                stations.Add(
                    new Station
                    {
                        Id = externalStationCode,
                        Name = match.Groups["stationName"].Value,
                        CountryCode = "NZ",
                        Coordinates = coordinates,
                    });
            }

            if (!dataFileLocationMapping.LocationIdToDataFileMappings.ContainsKey(location.Id))
            {
                dataFileLocationMapping.LocationIdToDataFileMappings.Add(location.Id, new List<DataFileFilterAndAdjustment>());
            }

            dataFileLocationMapping.LocationIdToDataFileMappings[location.Id].Add(dataFileFilterAndAdjustment);
        }


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
}
