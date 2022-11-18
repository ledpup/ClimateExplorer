using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ClimateExplorer.Analyser.StaticContent
{
    public class BuildStaticContent
    {
        public static async Task GenerateSiteMap()
        {
            var locations = await Location.GetLocations(false, @"Output\Location");

            var writer = XmlWriter.Create(@"Output\sitemap.xml");
            writer.WriteStartDocument();
            writer.WriteStartElement("sitemapindex", "http://www.sitemaps.org/schemas/sitemap/0.9");

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
            var locations = await Location.GetLocations(false, @"Output\Location");

            var indexTemplate = File.ReadAllText("StaticContent\\index-template.html");

           

            foreach (var location in locations)
            {
                var index = indexTemplate.Replace("***Title***", $"ClimateExplorer - {location.Name}")
                                         .Replace("***Url***", $"https://climateexplorer.net/location/{UrlReadyName(location)}");


                var path = $@"Output\location\IndexFiles\{UrlReadyName(location)}";
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

            writer.WriteStartElement("lastmod");
            writer.WriteValue(DateTime.Now.ToShortDateString());
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
