namespace ClimateExplorer.Core.Blog;

public interface IBlogService
{
    Task<IReadOnlyList<BlogPost>> GetAllPostsAsync();

    Task<BlogPost?> GetPostBySlugAsync(string slug);

    Task<BlogPost?> GetPreviousPostAsync(string slug);

    Task<BlogPost?> GetNextPostAsync(string slug);
}
