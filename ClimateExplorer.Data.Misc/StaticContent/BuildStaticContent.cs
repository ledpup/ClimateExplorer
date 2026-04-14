namespace ClimateExplorer.Data.Misc;

using System.Xml;
using ClimateExplorer.Core.Model;

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
        WriteTag(writer, "https://climateexplorer.net/locations");
        WriteTag(writer, "https://climateexplorer.net/about");
        WriteTag(writer, "https://climateexplorer.net/blog");

        var blogPostsDir = @"..\..\..\..\ClimateExplorer.Web\BlogPosts";
        foreach (var file in Directory.EnumerateFiles(blogPostsDir, "*.md").OrderBy(f => f))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.Length < 11 || fileName[10] != '-')
            {
                continue;
            }

            var dateStr = fileName[..10];
            var slug = fileName[11..];
            if (DateOnly.TryParse(dateStr, out var postDate))
            {
                WriteTagWithDate(writer, $"https://climateexplorer.net/blog/{slug}", postDate);
            }
        }

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

    private static void WriteTagWithDate(XmlWriter writer, string navigation, DateOnly lastModified)
    {
        writer.WriteStartElement("url");

        writer.WriteStartElement("loc");
        writer.WriteValue(navigation);
        writer.WriteEndElement();

        writer.WriteStartElement("lastmod");
        writer.WriteValue(lastModified.ToString("yyyy-MM-dd"));
        writer.WriteEndElement();

        writer.WriteEndElement();
    }
}
