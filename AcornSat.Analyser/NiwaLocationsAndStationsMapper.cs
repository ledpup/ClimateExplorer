using AcornSat.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AcornSat.Analyser;

public static class NiwaLocationsAndStationsMapper
{
    public static async Task BuildNiwaLocationsAsync(Guid dataSetDefintionId, string sourceLocationsFileName, string oldLocationsFileName)
    {
        var oldLocations = await Location.GetLocations($@"ReferenceData\NIWA\{oldLocationsFileName}");

        var locations = new List<Location>();
        var stations = new List<Station>();
        var dataFileLocationMapping = new DataLocationMapping() { DataSetDefinitionId = dataSetDefintionId };

        var regEx = new Regex(@"^(?<name>[\w|\s]*),(?<station>\d+),\w\d+\w?,(?<lat>-?\d+\.\d+),(?<lng>-?\d+\.\d+),(?<alt>-?\d+),.*,.*,(?<startdate>null|\d{4}\/\d{2}\/\d{2}),(?<enddate>null|\d{4}\/\d{2}\/\d{2})\s*,(?<adjustment>0|-?\d*\.\d*)\s*,(?<seriesName>[\w|'|\s|-]*),(?<stationName>[\w|\s]*)$");
        var locationRowData = File.ReadAllLines(@$"ReferenceData\NIWA\{sourceLocationsFileName}");

        // Create the initial list of locations
        foreach (var row in locationRowData)
        {
            var match = regEx.Match(row);

            var locationName = match.Groups["name"].Value;

            var location = locations.SingleOrDefault(x => x.Name == locationName);
            if (location == null)
            {
                location = new Location
                {
                    Id = Guid.NewGuid(),
                    Name = locationName,
                };
                locations.Add(location);
            }
            // The last entry will have the coordinates we'll use for this location. Just update them everytime.
            location.Coordinates = new Coordinates
            {
                Latitude = float.Parse(match.Groups["lat"].Value),
                Longitude = float.Parse(match.Groups["lng"].Value),
                Elevation = float.Parse(match.Groups["alt"].Value),
            };

            var oldLocation = oldLocations.SingleOrDefault(x => x.Name == location.Name);
            if (oldLocation != null)
            {
                location.Id = oldLocation.Id;
            }
            else
            {
                throw new Exception("Expecting that there would be no knew location added");
            }

            var externalStationCode = match.Groups["station"].Value;
            var dataFileFilterAndAdjustment = new DataFileFilterAndAdjustment
            {
                ExternalStationCode = externalStationCode,
                StartDate = match.Groups["startdate"].Value == "null" ? null : DateTime.Parse(match.Groups["startdate"].Value),
                EndDate = match.Groups["enddate"].Value == "null" ? null : DateTime.Parse(match.Groups["enddate"].Value),
                ValueAdjustment = float.Parse(match.Groups["adjustment"].Value)
            };

            if (!stations.Any(x => x.ExternalStationCode == externalStationCode))
            {
                stations.Add(new Station 
                { 
                    Name = match.Groups["stationName"].Value,
                    ExternalStationCode = externalStationCode 
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
        var path = new DirectoryInfo(@"Output\NIWA");
        if (!path.Exists)
        {
            path.Create();
        }
        File.WriteAllText(@"Output\NIWA\Locations.json", JsonSerializer.Serialize(locations, options));
        File.WriteAllText(@"Output\NIWA\Stations.json", JsonSerializer.Serialize(stations, options));
        File.WriteAllText(@"Output\NIWA\DataFileLocationMapping.json", JsonSerializer.Serialize(dataFileLocationMapping, options));
    }
}
