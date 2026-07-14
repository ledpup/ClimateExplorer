namespace ClimateExplorer.WebApi;

using System.Threading;
using ClimateExplorer.Core.DataPreparation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

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
        app.MapGet("/location-dataset-metadata", LocationEndpoints.GetLocationDataSetMetadata);
        app.MapGet("/nearby-locations", LocationEndpoints.GetNearbyLocations);
        app.MapGet("/country", MetadataEndpoints.GetCountries);
        app.MapGet("/region", LocationEndpoints.GetRegions);
        app.MapGet("/heating-score-table", MetadataEndpoints.GetHeatingScoreTable);
        app.MapGet("/climate-record", ClimateRecordsEndpoints.GetClimateRecords);

        // permitSourceUpdate is deliberately not a bindable parameter here: requests hitting /dataset
        // directly must never trigger a new external data download via ClimateExplorer.Data.Downloading.
        app.MapPost(
            "/dataset",
            (PostDataSetsRequestBody body, [FromServices] ClimateExplorerApiServices services, CancellationToken cancellationToken) =>
                DataSetEndpoints.PostDataSets(body, services, permitSourceUpdate: false, cancellationToken));
    }
}
