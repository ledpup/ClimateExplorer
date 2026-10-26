namespace ClimateExplorer.Web.UiLogic;

using ClimateExplorer.Web.UiModel;

/// <summary>
/// Builds the chart legend labels without repeating what the chart already shows elsewhere:
/// <list type="bullet">
/// <item>With one place on the chart, the location is left out. The chart title names it.</item>
/// <item>On desktop, a unit every series shares is left out. Every y axis title shows its unit.</item>
/// <item>On desktop, with two or more legend entries (trend overlays count), the other parts every entry shares are
/// left out of the labels and shown once in the series note. A trend's entry is compared by its prefix, the short
/// title, so parts it doesn't have (e.g. smoothing) stay on its series' label.</item>
/// </list>
/// The rules work on <see cref="SeriesLabelSegment"/>s, before they're joined, so parts are compared by what
/// they are as well as their text.
/// </summary>
public static class SeriesLabeller
{
    /// <param name="series">The rendered series, in chart order.</param>
    /// <param name="unitLabels">Each series' unit as displayed, from <see cref="SeriesDisplayUnits"/>.</param>
    /// <param name="hasTrends">Whether each series has a trend overlay on the chart.</param>
    /// <param name="isMobileDevice">Whether to use the short labels, which only leave out the location.</param>
    public static SeriesLabels Build(IReadOnlyList<ChartSeriesDefinition> series, IReadOnlyList<string> unitLabels, IReadOnlyList<bool> hasTrends, bool isMobileDevice)
    {
        var includeLocation = ChartLogic.GetPlaces(series).Length != 1;
        var shortTitles = series.Select(x => x.GetFriendlyTitleShortSegments(includeLocation)).ToList();

        if (isMobileDevice)
        {
            var shortLabels = shortTitles.Select(SeriesLabelSegment.Join).ToList();

            return new SeriesLabels(
                [.. series.Select((x, i) => x.GetTransformationOverrideLabel() ?? shortLabels[i])],
                shortLabels,
                null);
        }

        var labels = series.Select((x, i) => GetLabelSegments(x, unitLabels[i], includeLocation)).ToList();

        RemoveSharedUnit(labels);

        var shared = GetSharedSegments(labels, [.. shortTitles.Where((_, i) => hasTrends[i])]);
        var sharedParts = shared.Select(x => x.Part).ToHashSet();

        foreach (var label in labels)
        {
            label.RemoveAll(shared.Contains);
        }

        // The short title loses the same parts as the legend label. When that leaves nothing, the trend is named
        // by what's left of its series' legend label, so two trends can't both be just "{period} trend". With one
        // series there's nothing to confuse it with, so the prefix is left empty.
        var trendPrefixes = shortTitles
            .Select((title, i) =>
            {
                var prefix = title.Where(x => !sharedParts.Contains(x.Part)).ToList();
                return SeriesLabelSegment.Join(prefix.Count > 0 || series.Count == 1 ? prefix : labels[i]);
            })
            .ToList();

        return new SeriesLabels(
            [.. labels.Select(SeriesLabelSegment.Join)],
            trendPrefixes,
            shared.Count == 0 ? null : SeriesLabelSegment.Join(shared));
    }

    /// <summary>
    /// An override label (e.g. "First day of frost") replaces the whole label, unit included, and is never shared.
    /// </summary>
    private static List<SeriesLabelSegment> GetLabelSegments(ChartSeriesDefinition series, string unitLabel, bool includeLocation)
    {
        if (series.GetTransformationOverrideLabel() is { } overrideLabel)
        {
            return [new(SeriesLabelPart.Whole, overrideLabel)];
        }

        var segments = series.GetFriendlyTitleSegments(includeLocation);

        if (!string.IsNullOrWhiteSpace(unitLabel))
        {
            segments.Add(new(SeriesLabelPart.Unit, unitLabel));
        }

        return segments;
    }

    private static void RemoveSharedUnit(List<List<SeriesLabelSegment>> labels)
    {
        var units = labels.Select(x => x.SingleOrDefault(s => s.Part == SeriesLabelPart.Unit)).ToList();

        if (units.FirstOrDefault() is { } unit && units.All(x => x == unit))
        {
            labels.ForEach(x => x.Remove(unit));
        }
    }

    /// <summary>
    /// The segments every label and trend prefix has, in the first label's order. Never the location (with one
    /// place it's already gone, and with several it's what tells the series apart), the unit (a shared one is
    /// already gone, and one that differs says which axis to read), or a whole label.
    /// </summary>
    /// <param name="trendPrefixes">The short title of each series with a trend overlay.</param>
    private static List<SeriesLabelSegment> GetSharedSegments(List<List<SeriesLabelSegment>> labels, List<List<SeriesLabelSegment>> trendPrefixes)
    {
        if (labels.Count + trendPrefixes.Count < 2)
        {
            return [];
        }

        var shared = labels[0]
            .Where(x => x.Part is not (SeriesLabelPart.Location or SeriesLabelPart.Unit or SeriesLabelPart.Whole))
            .Where(x => labels.All(label => label.Contains(x)) && trendPrefixes.All(prefix => prefix.Contains(x)))
            .ToList();

        // Series that differ only in something the label doesn't show (e.g. bar and line) share every part.
        // Moving them all would leave those labels empty, so they keep the labels they'd have had.
        return labels.Any(label => label.All(shared.Contains)) ? [] : shared;
    }
}
