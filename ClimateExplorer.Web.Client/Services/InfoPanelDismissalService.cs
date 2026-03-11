namespace ClimateExplorer.Web.Services;

using Blazored.LocalStorage;

public interface IInfoPanelDismissalService
{
    Task<bool> ShouldShowAsync(string panelName, string version);
    Task DismissAsync(string panelName, string version);
}

public class InfoPanelDismissalService : IInfoPanelDismissalService
{
    private const string StorageKey = "infoPanelDismissals";
    private static readonly TimeSpan SameVersionCooldown = TimeSpan.FromDays(365);
    private static readonly TimeSpan NewVersionCooldown = TimeSpan.FromDays(90);

    private readonly ILocalStorageService localStorage;

    public InfoPanelDismissalService(ILocalStorageService localStorage)
    {
        this.localStorage = localStorage;
    }

    public async Task<bool> ShouldShowAsync(string panelName, string version)
    {
        var dismissals = await GetDismissalsAsync();
        var entry = dismissals.Find(d => d.Name == panelName);

        if (entry is null)
        {
            return true;
        }

        var elapsed = DateTime.UtcNow - entry.DismissedAtUtc;

        if (entry.Version == version)
        {
            return elapsed >= SameVersionCooldown;
        }

        return elapsed >= NewVersionCooldown;
    }

    public async Task DismissAsync(string panelName, string version)
    {
        var dismissals = await GetDismissalsAsync();
        var entry = dismissals.Find(d => d.Name == panelName);

        if (entry is not null)
        {
            entry.Version = version;
            entry.DismissedAtUtc = DateTime.UtcNow;
        }
        else
        {
            dismissals.Add(new InfoPanelDismissal
            {
                Name = panelName,
                Version = version,
                DismissedAtUtc = DateTime.UtcNow,
            });
        }

        await localStorage.SetItemAsync(StorageKey, dismissals);
    }

    private async Task<List<InfoPanelDismissal>> GetDismissalsAsync()
    {
        return await localStorage.GetItemAsync<List<InfoPanelDismissal>>(StorageKey) ?? [];
    }
}

public class InfoPanelDismissal
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime DismissedAtUtc { get; set; }
}
