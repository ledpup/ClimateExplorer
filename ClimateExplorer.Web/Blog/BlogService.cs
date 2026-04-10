namespace ClimateExplorer.Web.Blog;

using System.Text.RegularExpressions;
using Markdig;
using Microsoft.AspNetCore.Hosting;

public partial class BlogService
{
    private IReadOnlyList<BlogPost>? posts;

    public BlogService(IWebHostEnvironment env)
    {
        var postsDir = Path.Combine(env.ContentRootPath, "BlogPosts");
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        var postList = new List<BlogPost>();

        foreach (var file in Directory.EnumerateFiles(postsDir, "*.md").OrderBy(f => f))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            if (fileName.Length < 11 || fileName[10] != '-')
            {
                continue;
            }

            var dateStr = fileName[..10];
            var slug = fileName[11..];

            if (!DateOnly.TryParse(dateStr, out var date))
            {
                continue;
            }

            var raw = File.ReadAllText(file);
            var (frontMatter, body) = SplitFrontMatter(raw);
            var meta = ParseFrontMatter(frontMatter);

            var title = meta.GetValueOrDefault("title", slug).Trim('"', '\'', ' ');
            var category = meta.GetValueOrDefault("categories", string.Empty).Trim();

            var processedBody = PreprocessMarkdown(body, slug);
            var html = Markdown.ToHtml(processedBody, pipeline);

            postList.Add(new BlogPost
            {
                Slug = slug,
                Title = title,
                Date = date,
                Category = category,
                HtmlContent = html,
                Excerpt = ExtractExcerpt(html),
            });
        }

        posts = [.. postList.OrderByDescending(p => p.Date)];
    }

    public IReadOnlyList<BlogPost> GetAllPosts() => posts!;

    public BlogPost? GetPostBySlug(string slug) =>
        posts!.SingleOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

    private static (string FrontMatter, string Body) SplitFrontMatter(string raw)
    {
        var text = raw.Replace("\r\n", "\n").TrimStart();

        if (!text.StartsWith("---\n", StringComparison.Ordinal))
        {
            return (string.Empty, raw);
        }

        var end = text.IndexOf("\n---\n", 4, StringComparison.Ordinal);

        if (end < 0)
        {
            return (string.Empty, raw);
        }

        var frontMatter = text[4..end];
        var body = text[(end + 5)..];
        return (frontMatter, body);
    }

    private static Dictionary<string, string> ParseFrontMatter(string frontMatter)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in frontMatter.Split('\n'))
        {
            var colon = line.IndexOf(':', StringComparison.Ordinal);

            if (colon > 0)
            {
                dict[line[..colon].Trim()] = line[(colon + 1)..].Trim();
            }
        }

        return dict;
    }

    private static string PreprocessMarkdown(string markdown, string currentSlug)
    {
        // Replace {{site.url}}/blog/assets/ and /blog/assets/ with /blog-assets/
        var result = SiteUrlAssets().Replace(markdown, "/blog-assets/");
        result = PlainAssets().Replace(result, "/blog-assets/");

        // Replace {{site.baseurl}}{% post_url YYYY-MM-DD-slug %} with /blog/slug
        result = PostUrl().Replace(result, m =>
        {
            var fileName = m.Groups["file"].Value;
            var slug = fileName.Length > 11 ? fileName[11..] : fileName;
            return $"/blog/{slug}";
        });

        return result;
    }

    private static string ExtractExcerpt(string html)
    {
        var match = FirstParagraph().Match(html);

        if (!match.Success)
        {
            return string.Empty;
        }

        return StripTags().Replace(match.Groups[1].Value, string.Empty).Trim();
    }

    [GeneratedRegex(@"\{\{site\.url\}\}/blog/assets/")]
    private static partial Regex SiteUrlAssets();

    [GeneratedRegex(@"(?<![{%])/blog/assets/")]
    private static partial Regex PlainAssets();

    [GeneratedRegex(@"\{\{site\.baseurl\}\}\{%\s*post_url\s+(?<file>[\w.\-]+)\s*%\}")]
    private static partial Regex PostUrl();

    [GeneratedRegex(@"<p>(.*?)</p>", RegexOptions.Singleline)]
    private static partial Regex FirstParagraph();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex StripTags();
}
