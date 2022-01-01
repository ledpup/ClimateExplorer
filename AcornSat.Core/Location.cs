using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


public class Location
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<string> Sites { get; set; }
    public Coordinates Coordinates { get; set;}
    public Location()
    {
        Sites = new List<string>();
    }
    public static List<Location> GetLocations(string locationFilePath = "Locations.json")
    {
        var locationText = File.ReadAllText(locationFilePath);
        var locations = JsonSerializer.Deserialize<List<Location>>(locationText);
        return locations;
    }
}
