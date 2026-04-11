namespace ClimateExplorer.Web.Client.Services;

using System.Net.Http.Json;
using ClimateExplorer.Core.Blog;

public class ClientBlogService : IBlogService
{
    private readonly HttpClient http;
    private IReadOnlyList<BlogPost>? cache;

    public ClientBlogService(HttpClient http) => this.http = http;

    public async Task<IReadOnlyList<BlogPost>> GetAllPostsAsync() => await EnsureLoadedAsync();

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        var all = await EnsureLoadedAsync();
        return all.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<BlogPost?> GetPreviousPostAsync(string slug)
    {
        var all = await EnsureLoadedAsync();
        var list = all.ToList();
        var index = list.FindIndex(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
        return index > 0 ? list[index - 1] : null;
    }

    public async Task<BlogPost?> GetNextPostAsync(string slug)
    {
        var all = await EnsureLoadedAsync();
        var list = all.ToList();
        var index = list.FindIndex(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index < list.Count - 1 ? list[index + 1] : null;
    }

    private async Task<IReadOnlyList<BlogPost>> EnsureLoadedAsync()
    {
        cache ??= await http.GetFromJsonAsync<List<BlogPost>>("/api/blog/posts") ?? [];
        return cache;
    }
}
