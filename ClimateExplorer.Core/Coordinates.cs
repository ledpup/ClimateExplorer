public struct Coordinates
{
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public float Elevation { get; set; }

    public override string ToString()
    {
        return $"{Math.Round(Latitude, 1)}°, {Math.Round(Longitude, 1)}°, {Math.Round(Elevation, 1)}m";
    }
    public string ToString(bool prefix = false)
    {
        if (prefix)
        {
            return $"Lat {Math.Round(Latitude, 1)}° Long {Math.Round(Longitude, 1)}° Ele {Math.Round(Elevation, 1)}m";
        }
        return this.ToString();
    }
}
