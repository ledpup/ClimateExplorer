namespace ClimateExplorer.Web.Client.Components.Common;

using System.Globalization;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Maps the string a <c>&lt;option value&gt;</c> carries back to the <typeparamref name="TValue"/> it was made from,
/// so <see cref="Select{TValue}"/> can raise the original value instead of parsing the string.
/// </summary>
/// <remarks>
/// Registrations are counted per key. When Blazor swaps one <see cref="SelectItem{TValue}"/> for another with the
/// same value, the new item registers before the old one is disposed, and the key must survive that.
/// </remarks>
public sealed class SelectValueMap<TValue>
{
    private readonly Dictionary<string, (TValue Value, int Count)> entries = new(StringComparer.Ordinal);

    public static string KeyFor(TValue value)
    {
        return Convert.ToString(BindConverter.FormatValue(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public string Register(TValue value)
    {
        var key = KeyFor(value);
        entries[key] = entries.TryGetValue(key, out var entry)
            ? (entry.Value, entry.Count + 1)
            : (value, 1);

        return key;
    }

    public void Unregister(TValue value)
    {
        var key = KeyFor(value);
        if (!entries.TryGetValue(key, out var entry))
        {
            return;
        }

        if (entry.Count <= 1)
        {
            entries.Remove(key);
        }
        else
        {
            entries[key] = (entry.Value, entry.Count - 1);
        }
    }

    public bool TryGetValue(string? key, out TValue value)
    {
        if (key is not null && entries.TryGetValue(key, out var entry))
        {
            value = entry.Value;
            return true;
        }

        value = default!;
        return false;
    }
}
