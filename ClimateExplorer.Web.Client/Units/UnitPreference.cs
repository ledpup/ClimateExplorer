namespace ClimateExplorer.Web.Client.Units;

using Blazored.LocalStorage;
using Microsoft.JSInterop;

/// <summary>
/// The reader's unit system, remembered in localStorage. Scoped, so each reader has their own.
/// </summary>
/// <remarks>
/// localStorage can only be read in the browser, so the server prerender is always metric and a US
/// customary reader sees metric text until <see cref="LoadAsync"/> runs once the page is interactive.
/// <c>Routes</c> calls it on its first interactive render and cascades <see cref="Formatter"/> to every
/// component.
/// </remarks>
public sealed class UnitPreference
{
    private const string StorageKey = "unitSystem";

    private readonly ILocalStorageService localStorage;
    private Task<bool>? loading;

    public UnitPreference(ILocalStorageService localStorage)
    {
        this.localStorage = localStorage;
    }

    public event Action? Changed;

    public IUnitFormatter Formatter { get; private set; } = UnitFormatter.Metric;

    /// <summary>
    /// Reads the stored choice. Every caller awaits the same read, so a component that must not draw in
    /// the wrong units (the chart) can wait for it without reading storage twice.
    /// </summary>
    public async Task LoadAsync()
    {
        loading ??= LoadFromStorageAsync();

        // Not read yet (prerendering): forget this attempt so the interactive render's call reads it.
        if (!await loading)
        {
            loading = null;
        }
    }

    public async Task SetAsync(UnitSystem system)
    {
        Apply(system);

        try
        {
            await localStorage.SetItemAsStringAsync(StorageKey, system.ToString());
        }
        catch (JSException)
        {
            // Storage can be blocked (e.g. some private windows). The choice still applies to this visit.
        }
    }

    /// <returns>False when storage couldn't be read yet, because JavaScript isn't available while prerendering.</returns>
    private async Task<bool> LoadFromStorageAsync()
    {
        try
        {
            var stored = await localStorage.GetItemAsStringAsync(StorageKey);

            if (Enum.TryParse<UnitSystem>(stored, out var system) && Enum.IsDefined(system))
            {
                Apply(system);
            }

            return true;
        }
        catch (JSException)
        {
            // Unreadable storage means the default, metric.
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void Apply(UnitSystem system)
    {
        var formatter = UnitFormatter.For(system);

        if (ReferenceEquals(formatter, Formatter))
        {
            return;
        }

        Formatter = formatter;
        Changed?.Invoke();
    }
}
