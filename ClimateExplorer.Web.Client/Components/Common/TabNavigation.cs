namespace ClimateExplorer.Web.Client.Components.Common;

/// <summary>
/// Keyboard movement within a tab strip (ARIA tabs pattern): ArrowLeft/ArrowRight move to the previous/next enabled
/// tab, wrapping at the ends, and Home/End move to the first/last enabled tab.
/// </summary>
public static class TabNavigation
{
    public static bool IsNavigationKey(string key) =>
        key is "ArrowLeft" or "ArrowRight" or "Home" or "End";

    /// <summary>
    /// The index of the tab to move to from <paramref name="current"/>, or <paramref name="current"/> when the key
    /// isn't a navigation key or there's no enabled tab to move to.
    /// </summary>
    public static int NextIndex(int current, int count, Func<int, bool> isDisabled, string key)
    {
        // Home/End search from just past the opposite end, so the first/last tab itself is a candidate
        var next = key switch
        {
            "ArrowRight" => FindEnabled(current, count, isDisabled, 1),
            "ArrowLeft" => FindEnabled(current, count, isDisabled, -1),
            "Home" => FindEnabled(count - 1, count, isDisabled, 1),
            "End" => FindEnabled(0, count, isDisabled, -1),
            _ => null,
        };

        return next ?? current;
    }

    private static int? FindEnabled(int start, int count, Func<int, bool> isDisabled, int direction)
    {
        var index = start;
        for (var i = 0; i < count; i++)
        {
            index = (index + direction + count) % count;
            if (!isDisabled(index))
            {
                return index;
            }
        }

        return null;
    }
}
