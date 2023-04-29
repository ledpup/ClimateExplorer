namespace ClimateExplorer.Data.Isd;

public class StationHistoryFileProcessor
{
    public static async Task<Dictionary<string, Station>> Transform(Dictionary<string, Country> countries, short beginBeforeOrEqualTo, short endNoLaterThan)
    {
        var stationHistoryFile = (await File.ReadAllLinesAsync(@"SiteMetaData\isd-history.csv")).Skip(1);

        var stations = new Dictionary<string, Station>();

        foreach (var stationRow in stationHistoryFile)
        {
            var fields = stationRow.Split(',');
            var usaf = fields[0];
            var wban = fields[1];
            var stationName = fields[2];
            var countryCode = fields[3];
            var state = fields[4];
            var icao = fields[5];
            var lat = fields[6];
            var lng = fields[7];
            var elevation = fields[8];
            var begin = ConvertFieldToDate(fields[9]);
            var end = ConvertFieldToDate(fields[10]);
            if (!(begin.Year <= beginBeforeOrEqualTo && end.Year > endNoLaterThan))
            {
                continue;
            }

            countries.TryGetValue(countryCode, out Country? country);

            var id = usaf == "999999" ? wban : usaf;

            var station = new Station
            {
                Id = id,
                IdType = usaf == "999999" ? IdTypes.Wban : IdTypes.Usaf,
                Wban = wban,
                Usaf = usaf,
                Country = country,
                Name = stationName,
                Begin = begin,
                End = end,
            };

            if (!string.IsNullOrWhiteSpace(lat) && !string.IsNullOrWhiteSpace(lng))
            {
                station.Coordinates = new Coordinates
                {
                    Latitude = Convert.ToSingle(lat),
                    Longitude = Convert.ToSingle(lng),
                    Elevation = string.IsNullOrWhiteSpace(elevation) ? null : Convert.ToSingle(elevation)
                };
            }

            stations.Add(id, station);
        }

        return stations;
    }

    static DateOnly ConvertFieldToDate(string field)
    {
        return new DateOnly(Convert.ToInt16(field.Substring(0, 4)),
                            Convert.ToInt16(field.Substring(4, 2)),
                            Convert.ToInt16(field.Substring(6, 2)));
    }
}

public class Station
{
    public string? Id { get; set; }
    public IdTypes IdType { get; set; }
    public string Wban { get; set; }
    public string Usaf { get; set; }
    public string? Name { get; set; }
    public Country? Country { get; set; }
    public DateOnly Begin { get; set; }
    public DateOnly End { get; set; }
    public int Age
    { 
        get
        {
            return End.Year - Begin.Year;
        } 
    }
    public Coordinates Coordinates { get; set; }

    public List<StationDistance> StationDistances { get; set; }

    public double AverageDistance { get; set; }

    public double Score { get; set; }
    public override string ToString()
    {
        return $"{Name}, {Country}, {Coordinates.Latitude}, {Coordinates.Longitude}";
    }
}

public struct Coordinates
{
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public float? Elevation { get; set; }
}

public enum IdTypes
{
    Usaf,
    Wban
}
