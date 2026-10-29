namespace ClimateExplorer.Web.Client.Components.Common;

/// <summary>
/// The matching behind <see cref="Autocomplete{TItem, TValue}"/>: a case-insensitive "contains" on each item's text,
/// with the items whose text starts with the query ranked first.
/// </summary>
public static class AutocompleteFilter
{
    /// <summary>
    /// The items whose text contains <paramref name="query"/> (trimmed, ignoring case), prefix matches first and then
    /// the rest, each group in data order. Empty when the trimmed query is shorter than <paramref name="minLength"/>.
    /// At most <paramref name="max"/> items are returned; <paramref name="capped"/> says whether more matched.
    /// </summary>
    public static IReadOnlyList<TItem> Filter<TItem>(
        IEnumerable<TItem> data,
        Func<TItem, string> text,
        string? query,
        int minLength,
        int max,
        out bool capped)
    {
        capped = false;

        var trimmed = query?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length < minLength || max <= 0)
        {
            return [];
        }

        // Each group only needs max items: the prefix matches alone may fill the result
        var prefixMatches = new List<TItem>();
        var otherMatches = new List<TItem>();
        var matchCount = 0;

        foreach (var item in data)
        {
            var itemText = text(item) ?? string.Empty;
            var index = itemText.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            matchCount++;

            var group = index == 0 ? prefixMatches : otherMatches;
            if (group.Count < max)
            {
                group.Add(item);
            }
        }

        capped = matchCount > max;

        return prefixMatches.Concat(otherMatches).Take(max).ToList();
    }
}
