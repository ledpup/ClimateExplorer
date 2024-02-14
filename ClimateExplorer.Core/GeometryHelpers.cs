namespace ClimateExplorer.Core;

using GeoCoordinatePortable;

/// <summary>
/// Helper methods for working with geocoordinates.
/// </summary>
public static class GeometryHelpers
{
    // Gives a Rhumb bearing to follow to reach p2 starting from p1 (i.e. a constant bearing that could be followed all the way -
    // a great circle bearing requires bearing changes on the way).
    // Based on https://stackoverflow.com/questions/2042599/direction-between-2-latitude-longitude-points-in-c-sharp.
    public static double GetRhumbBearingFromPointToPoint(GeoCoordinate p1, GeoCoordinate p2)
    {
        var dLon = DegreesToRadians(p2.Longitude - p1.Longitude);

        var dPhi =
            Math.Log(
                Math.Tan(
                    (DegreesToRadians(p2.Latitude) / 2) + (Math.PI / 4))
                /
                Math.Tan(
                    (DegreesToRadians(p1.Latitude) / 2) + (Math.PI / 4)));

        if (Math.Abs(dLon) > Math.PI)
        {
            dLon = dLon > 0 ? -((2 * Math.PI) - dLon) : ((2 * Math.PI) + dLon);
        }

        return ToBearingDegrees(Math.Atan2(dLon, dPhi));
    }

    // Returns the depth-3 compass rose direction name (e.g. ENE, S, SSW, etc.) for the given bearing in degrees
    public static string GetCompassRoseDirectionName(double bearing)
    {
        var normalisedBearing = ((bearing % 360) + 360) % 360;

        string[] roseNames = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };

        double subdivisionSize = 360.0 / roseNames.Length;

        for (int i = 0; i < roseNames.Length + 1; i++)
        {
            if (normalisedBearing < subdivisionSize * (i + 0.5))
            {
                return roseNames[i % roseNames.Length];
            }
        }

        throw new Exception("Could not convert bearing " + bearing + " to a compass rose name direction name");
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180 / Math.PI;
    }

    private static double ToBearingDegrees(double radians)
    {
        return (RadiansToDegrees(radians) + 360) % 360;
    }
}
