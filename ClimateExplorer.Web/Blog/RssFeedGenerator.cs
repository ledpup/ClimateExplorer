namespace ClimateExplorer.Web.Blog;

using System.Text;
using System.Xml;
using ClimateExplorer.Core.Blog;

public static class RssFeedGenerator
{
    private const string BaseUrl = "https://climateexplorer.net";
    private const string AtomNs = "http://www.w3.org/2005/Atom";

    public static byte[] Build(IReadOnlyList<BlogPost> posts)
    {
        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };

        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();

            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteAttributeString("xmlns", "atom", null, AtomNs);

            writer.WriteStartElement("channel");

            writer.WriteElementString("title", "ClimateExplorer Blog");
            writer.WriteElementString("link", $"{BaseUrl}/blog");
            writer.WriteElementString("description", "Updates on features, data sets, and climate concepts.");
            writer.WriteElementString("language", "en-us");

            writer.WriteStartElement("atom", "link", AtomNs);
            writer.WriteAttributeString("href", $"{BaseUrl}/blog/rss.xml");
            writer.WriteAttributeString("rel", "self");
            writer.WriteAttributeString("type", "application/rss+xml");
            writer.WriteEndElement();

            foreach (var post in posts)
            {
                var url = $"{BaseUrl}/blog/{post.Slug}";
                var dt = post.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                var pubDate = dt.ToString(
                    "ddd, dd MMM yyyy HH:mm:ss +0000",
                    System.Globalization.CultureInfo.InvariantCulture);

                writer.WriteStartElement("item");
                writer.WriteElementString("title", post.Title);
                writer.WriteElementString("link", url);
                if (!string.IsNullOrEmpty(post.Excerpt))
                {
                    writer.WriteElementString("description", post.Excerpt);
                }

                writer.WriteElementString("pubDate", pubDate);
                writer.WriteStartElement("guid");
                writer.WriteAttributeString("isPermaLink", "true");
                writer.WriteString(url);
                writer.WriteEndElement(); // guid
                if (!string.IsNullOrEmpty(post.Category))
                {
                    writer.WriteElementString("category", post.Category);
                }

                writer.WriteEndElement(); // item
            }

            writer.WriteEndElement(); // channel
            writer.WriteEndElement(); // rss
            writer.WriteEndDocument();
        }

        return ms.ToArray();
    }
}
