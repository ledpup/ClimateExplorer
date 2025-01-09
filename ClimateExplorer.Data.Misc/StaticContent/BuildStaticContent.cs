namespace ClimateExplorer.Data.Misc;

using ClimateExplorer.Core.Model;
using System.Xml;

public class BuildStaticContent
{
    public static async Task GenerateSiteMap()
    {
        var locations = await Location.GetLocations();

        var writer = XmlTextWriter.Create(@"..\..\..\..\ClimateExplorer.Web\wwwroot\sitemap.xml", new XmlWriterSettings { Indent = true, NewLineOnAttributes = true });

        writer.WriteStartDocument();
        writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

        WriteTag(writer, "https://climateexplorer.net/");
        WriteTag(writer, "https://climateexplorer.net/regionalandglobal");
        WriteTag(writer, "https://climateexplorer.net/about");
        WriteTag(writer, "https://climateexplorer.net/blog");
        WriteTag(writer, "https://climateexplorer.net/blog/about");

        var distinctLocations = locations.Select(x => x.UrlReadyName()).Distinct();
        foreach (var locationName in distinctLocations)
        {
            WriteTag(writer, $"https://climateexplorer.net/location/{locationName}");
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Close();
    }

    private static void WriteTag(XmlWriter writer, string navigation)
    {
        writer.WriteStartElement("url");

        writer.WriteStartElement("loc");
        writer.WriteValue(navigation);
        writer.WriteEndElement();

        writer.WriteEndElement();
    }
}
