using System.Diagnostics.CodeAnalysis;

public struct Coordinates
{
    public Coordinates(float lat, float lng)
    {
        Latitude = lat;
        Longitude = lng;
        Elevation = 0;
    }

    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public float? Elevation { get; set; }

    public bool Equals(Coordinates obj) => Latitude == obj.Latitude && Longitude == obj.Longitude && Elevation == obj.Elevation;
    public static bool operator ==(Coordinates lhs, Coordinates rhs) => lhs.Equals(rhs);
    public static bool operator !=(Coordinates lhs, Coordinates rhs) => !(lhs == rhs);

    public override string ToString()
    {
        if (Elevation == null)
        {
            return $"{Math.Round(Latitude, 1)}°, {Math.Round(Longitude, 1)}°";
        }
        return $"{Math.Round(Latitude, 1)}°, {Math.Round(Longitude, 1)}°, {Math.Round(Elevation.Value, 1)}m";
    }

    public string ToString(bool prefix = false)
    {
        if (prefix)
        {
            if (Elevation == null)
            {
                return $"Lat {Math.Round(Latitude, 1)}° Long {Math.Round(Longitude, 1)}°";
            }
            return $"Lat {Math.Round(Latitude, 1)}° Long {Math.Round(Longitude, 1)}° Ele {Math.Round(Elevation.Value, 1)}m";
        }
        return this.ToString();
    }
}
