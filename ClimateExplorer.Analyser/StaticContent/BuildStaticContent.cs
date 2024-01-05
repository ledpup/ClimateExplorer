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
        WriteTag(writer, "https://climateexplorer.net/regionalandglobal");
        WriteTag(writer, "https://climateexplorer.net/about");
        WriteTag(writer, "https://climateexplorer.net/blog");
        WriteTag(writer, "https://climateexplorer.net/blog/about");
        foreach (var location in locations)
        {
            WriteTag(writer, $"https://climateexplorer.net/location/{location.UrlReadyName()}");
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Close();
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
