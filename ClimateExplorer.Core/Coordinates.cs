public struct Coordinates
{
    public Coordinates(double lat, double lng)
    {
        Latitude = lat;
        Longitude = lng;
        Elevation = 0;
    }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Elevation { get; set; }

    public static bool operator ==(Coordinates lhs, Coordinates rhs) => lhs.Equals(rhs);
    public static bool operator !=(Coordinates lhs, Coordinates rhs) => !(lhs == rhs);

    public readonly bool Equals(Coordinates obj) => Latitude == obj.Latitude && Longitude == obj.Longitude && Elevation == obj.Elevation;

    public override readonly string ToString()
    {
        if (Elevation == null)
        {
            return $"{Math.Round(Latitude, 0)}° {Math.Round(Longitude, 0)}°";
        }

        return $"{Math.Round(Latitude, 0)}° {Math.Round(Longitude, 0)}° {Math.Round(Elevation.Value, 0)}m";
    }

    public string ToFriendlyString(bool prefix = false)
    {
        if (prefix)
        {
            if (Elevation == null)
            {
                return $"Lat {Math.Round(Latitude, 1)}° Long {Math.Round(Longitude, 1)}°";
            }

            return $"Lat {Math.Round(Latitude, 1)}° Long {Math.Round(Longitude, 1)}° Ele {Math.Round(Elevation.Value, 1)}m";
        }

        return ToString();
    }

    public override readonly bool Equals(object? obj) => Equals((Coordinates)obj!);

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
