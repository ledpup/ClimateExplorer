namespace ClimateExplorer.WebApi;

using Microsoft.AspNetCore.Builder;

internal static class ClimateExplorerEndpointRouteBuilderExtensions
{
    internal static void MapClimateExplorerEndpoints(this WebApplication app)
    {
        app.MapGet("/", MetadataEndpoints.GetRoot);
        app.MapGet("/about", MetadataEndpoints.GetAbout);
        app.MapGet("/datasetdefinition", MetadataEndpoints.GetDataSetDefinitions);
        app.MapGet("/location", LocationEndpoints.GetLocations);
        app.MapGet("/location-by-path", LocationEndpoints.GetLocationByPath);
        app.MapGet("/location-by-id", LocationEndpoints.GetLocationById);
        app.MapGet("/nearby-locations", LocationEndpoints.GetNearbyLocations);
        app.MapGet("/country", MetadataEndpoints.GetCountries);
        app.MapGet("/region", LocationEndpoints.GetRegions);
        app.MapGet("/heating-score-table", MetadataEndpoints.GetHeatingScoreTable);
        app.MapGet("/climate-record", ClimateRecordsEndpoints.GetClimateRecords);
        app.MapGet("/latest-record", LatestRecordsEndpoints.GetLatestRecords);
        app.MapPost("/dataset", DataSetEndpoints.PostDataSets);
    }
}
