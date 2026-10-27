namespace ClimateExplorer.Web.UiModel;

/// <summary>One part of a series label, such as its location or data type.</summary>
public sealed record SeriesLabelSegment(SeriesLabelPart Part, string Text)
{
    public const string Separator = " | ";

    public static string Join(IEnumerable<SeriesLabelSegment> segments) => string.Join(Separator, segments.Select(x => x.Text));
}
