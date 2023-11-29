using ClimateExplorer.Core.Model;
using System.Xml;

namespace ClimateExplorer.Analyser.StaticContent;

public class BuildStaticContent
{
    public static async Task GenerateSiteMap()
    {
        Directory.CreateDirectory(@"Output\Location");

        var locations = await Location.GetLocations(@"Output\Location");

        var writer = XmlTextWriter.Create(@"Output\sitemap.xml", new XmlWriterSettings { Indent = true, NewLineOnAttributes = true });

        writer.WriteStartDocument();
        writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

        
        WriteTag(writer, "https://climateexplorer.net/");
        WriteTag(writer, "https://climateexplorer.net/about");
        WriteTag(writer, "https://climateexplorer.net/blog");
        WriteTag(writer, "https://climateexplorer.net/blog/about");
        WriteTag(writer, "https://climateexplorer.net/blog/locations");
        foreach (var location in locations)
        {
            WriteTag(writer, $"https://climateexplorer.net/location/{UrlReadyName(location)}");
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Close();
    }

    public static async Task GenerateIndexFiles()
    {
        var locations = await Location.GetLocations(@"Output\Location");

        var indexTemplate = File.ReadAllText("StaticContent\\index-template.html");

        var rootIndex = indexTemplate.Replace("***Title***", $"Explore long-term climate trends")
                                     .Replace("***Url***", $"https://climateexplorer.net/");

        var rootIndexPath = $@"Output\IndexFiles";
        Directory.CreateDirectory(rootIndexPath);
        File.WriteAllText($@"{rootIndexPath}\index.html", rootIndex);

        foreach (var location in locations)
        {
            var index = indexTemplate.Replace("***Title***", $"ClimateExplorer - {location.Name}")
                                     .Replace("***Url***", $"https://climateexplorer.net/location/{UrlReadyName(location)}");


            var path = $@"Output\IndexFiles\Locations\{UrlReadyName(location)}";
            Directory.CreateDirectory(path);
            File.WriteAllText($@"{path}\index.html", index);
        }
    }

    private static string UrlReadyName(Location location)
    {
        return location.Name.ToLower().Replace(" ", "-");
    }

    private static void WriteTag(XmlWriter writer, string Navigation)
    {
        writer.WriteStartElement("url");

        writer.WriteStartElement("loc");
        writer.WriteValue(Navigation);
        writer.WriteEndElement();

        writer.WriteEndElement();
    }
}
