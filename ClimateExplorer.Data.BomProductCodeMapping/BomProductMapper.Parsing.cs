using System.Text.RegularExpressions;

public static partial class BomProductMapper
{
    static string? ExtractStationId(string html)
    {
        if (string.IsNullOrEmpty(html)) return null;
        var lower = html.ToLowerInvariant();
        var idx = lower.IndexOf("source of data", StringComparison.Ordinal);
        string snippet = html;
        if (idx >= 0)
        {
            snippet = html.Substring(idx, Math.Min(800, html.Length - idx));
        }

        var m = Regex.Match(snippet, "\\{?station\\s+0*(\\d{4,6})\\}?", RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value;

        m = Regex.Match(snippet, @"station[^\n\r\t\<\>\d]*([0-9]{5,6})", RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value;

        return null;
    }

    static string ExtractStationName(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var lower = html.ToLowerInvariant();
        var idx = lower.IndexOf("observations were drawn from", StringComparison.Ordinal);
        string snippet = html;
        if (idx >= 0)
        {
            snippet = html.Substring(idx, Math.Min(400, html.Length - idx));
        }

        var m = Regex.Match(snippet, @"Observations were drawn from\s+([^\{\n\<]+)", RegexOptions.IgnoreCase);
        if (m.Success) return HtmlToPlainText(m.Groups[1].Value).Trim();

        m = Regex.Match(html, @"<h2[^>]*>\s*Source of data\s*</h2>.*?<p[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (m.Success) return HtmlToPlainText(m.Groups[1].Value).Trim();

        return string.Empty;
    }

    static string HtmlToPlainText(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var noTags = Regex.Replace(s, @"<.*?>", " ");
        return System.Net.WebUtility.HtmlDecode(noTags).Replace("\n", " ").Replace("\r", " ").Trim();
    }

    static bool NamesRoughlyMatch(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        a = a.ToLowerInvariant(); b = b.ToLowerInvariant();
        if (a.Contains(b) || b.Contains(a)) return true;
        var strip = new[] { "airport", "aero", "station", "met", "observatory" };
        foreach (var w in strip)
        {
            a = a.Replace(w, ""); b = b.Replace(w, "");
        }
        a = Regex.Replace(a, "\\s+", " ").Trim();
        b = Regex.Replace(b, "\\s+", " ").Trim();
        if (a.Contains(b) || b.Contains(a)) return true;
        return false;
    }
}
