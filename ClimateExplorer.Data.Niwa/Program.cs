using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Niwa;

var niwaStations = await Station.GetStationsFromFiles(
    [
        $@"ReferenceMetaData\NIWA\Stations_NewZealand_7stations_unadjusted.json",
        $@"ReferenceMetaData\NIWA\Stations_NewZealand_11stations.json",
    ]);

await NiwaCliFloClient.GetDataForEachStation(niwaStations!);

await NiwaLocationsAndStationsMapper.BuildNiwaLocationsAsync(Guid.Parse("7522E8EC-E743-4CB0-BC65-6E9F202FC824"), "7-stations_locations_adjusted.csv", "7-stations_Locations.json", "_NewZealand_7stations_adjusted");
await NiwaLocationsAndStationsMapper.BuildNiwaLocationsAsync(Guid.Parse("534950DC-EDA4-4DB5-8816-3705358F1797"), "7-stations_locations_unadjusted.csv", "7-stations_Locations.json", "_NewZealand_7stations_unadjusted");
await NiwaLocationsAndStationsMapper.BuildNiwaLocationsAsync(Guid.Parse("88e52edd-3c67-484a-b614-91070037d47a"), "11-stations_locations.csv", "11-stations_Locations.json", "_NewZealand_11stations");