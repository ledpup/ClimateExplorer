namespace ClimateExplorer.Web.Blog;

using System.Text.RegularExpressions;
using ClimateExplorer.Core.Blog;
using Markdig;
using Microsoft.AspNetCore.Hosting;

public class BlogService : IBlogService
{
    private static readonly Regex SiteUrlAssetsRegex = new(@"\{\{site\.url\}\}/blog/assets/", RegexOptions.Compiled);
    private static readonly Regex PlainAssetsRegex = new(@"(?<![{%])/blog/assets/", RegexOptions.Compiled);
    private static readonly Regex PostUrlRegex = new(@"\{\{site\.baseurl\}\}\{%\s*post_url\s+(?<file>[\w.\-]+)\s*%\}", RegexOptions.Compiled);
    private static readonly Regex ExternalLinksRegex = new(@"<a href=""(https?://[^""]+)""", RegexOptions.Compiled);
    private static readonly Regex FirstParagraphRegex = new(@"<p>(.*?)</p>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex StripTagsRegex = new(@"<[^>]+>", RegexOptions.Compiled);

    private readonly IReadOnlyList<BlogPost> posts;

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

            var processedBody = PreprocessMarkdown(body);
            var html = ExternalLinksRegex.Replace(Markdown.ToHtml(processedBody, pipeline), @"<a href=""$1"" target=""_blank"" rel=""noopener noreferrer""");

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

    public Task<IReadOnlyList<BlogPost>> GetAllPostsAsync() => Task.FromResult(posts);

    public Task<BlogPost?> GetPostBySlugAsync(string slug) =>
        Task.FromResult(posts.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase)));

    public Task<BlogPost?> GetPreviousPostAsync(string slug)
    {
        var currentIndex = posts.ToList().FindIndex(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(currentIndex > 0 && currentIndex < posts.Count ? posts[currentIndex - 1] : null);
    }

    public Task<BlogPost?> GetNextPostAsync(string slug)
    {
        var currentIndex = posts.ToList().FindIndex(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(currentIndex >= 0 && currentIndex < posts.Count - 1 ? posts[currentIndex + 1] : null);
    }

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

    private static string PreprocessMarkdown(string markdown)
    {
        var result = SiteUrlAssetsRegex.Replace(markdown, "/blog-assets/");
        result = PlainAssetsRegex.Replace(result, "/blog-assets/");
        result = PostUrlRegex.Replace(result, m =>
        {
            var fileName = m.Groups["file"].Value;
            var slug = fileName.Length > 11 ? fileName[11..] : fileName;
            return $"/blog/{slug}";
        });

        return result;
    }

    private static string ExtractExcerpt(string html)
    {
        var match = FirstParagraphRegex.Match(html);

        if (!match.Success)
        {
            return string.Empty;
        }

        return StripTagsRegex.Replace(match.Groups[1].Value, string.Empty).Trim();
    }
}
