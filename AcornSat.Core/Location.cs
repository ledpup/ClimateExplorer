using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Location
{
    public string Name { get; set; }
    public string PrimarySite { get; set; }
    public string State { get; set; }

    public static List<Location> GetLocations(string locationFilePath)
    {
        var locations = new List<Location>();

        var locationRowData = File.ReadAllLines(locationFilePath);
        foreach (var row in locationRowData)
        {
            var splitRow = row.Split(',');
            locations.Add(new Location { Name = splitRow[0], PrimarySite = splitRow[1] });
        }
        return locations;
    }
}
