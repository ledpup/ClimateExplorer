namespace ClimateExplorer.Core.Blog;

public interface IBlogService
{
    IReadOnlyList<BlogPost> GetAllPosts();

    BlogPost? GetPostBySlug(string slug);
}
