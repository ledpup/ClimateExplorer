namespace ClimateExplorer.Web.Client.Components.Location;

using ClimateExplorer.Core.Model;
using GeoCoordinatePortable;
using Microsoft.JSInterop;

/// <summary>
/// Asks the browser for the user's position (the browser shows its own permission prompt) and
/// picks the closest known location.
/// </summary>
public static class BrowserGeolocation
{
    public static async Task<NearestLocationResult> FindNearestLocationAsync(IJSRuntime jsRuntime, IEnumerable<Location> locations)
    {
        var getLocationResult = await jsRuntime.InvokeAsync<GetLocationResult>("getLocation");

        if (getLocationResult.ErrorCode > 0)
        {
            var errorMessage = "Unable to determine your location" + (!string.IsNullOrWhiteSpace(getLocationResult.ErrorMessage) ? $" ({getLocationResult.ErrorMessage})" : string.Empty);
            return new NearestLocationResult(null, errorMessage);
        }

        var geoCoord = new GeoCoordinate(getLocationResult.Latitude, getLocationResult.Longitude);

        var distances = Location.GetDistances(geoCoord, locations);
        var closestLocation = distances.OrderBy(x => x.Distance).First();

        return new NearestLocationResult(closestLocation.LocationId, null);
    }

    private class GetLocationResult
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public float ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
