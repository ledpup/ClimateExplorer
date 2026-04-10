namespace ClimateExplorer.Web.Blog;

public class BlogPost
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required DateOnly Date { get; init; }
    public string Category { get; init; } = string.Empty;
    public required string HtmlContent { get; init; }
    public string Excerpt { get; init; } = string.Empty;
}
