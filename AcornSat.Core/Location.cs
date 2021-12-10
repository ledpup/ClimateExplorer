using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Location
{
    public string Name { get; set; }
    public string PrimarySiteId { get; set; }
    public string State { get; set; }
    public List<string> Sites { get; set; }
    public string AdjustedSiteName;
    public Location()
    {
        Sites = new List<string>();
    }
    public static List<Location> GetLocations(string locationFilePath)
    {
        var locations = new List<Location>();

        var locationRowData = File.ReadAllLines(locationFilePath);
        foreach (var row in locationRowData)
        {
            var splitRow = row.Split(',');
            var location = new Location { Name = splitRow[0], PrimarySiteId = splitRow[1] };
            location.Sites.Add(location.PrimarySiteId);
            if (splitRow.Length > 2 && !string.IsNullOrWhiteSpace(splitRow[2]))
            {
                location.Sites.Add(splitRow[2]);
            }
            locations.Add(location);
        }
        return locations;
    }
}
