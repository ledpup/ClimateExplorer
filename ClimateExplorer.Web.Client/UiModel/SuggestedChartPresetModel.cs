namespace ClimateExplorer.Web.UiModel;

public class SuggestedChartPresetModel
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool MenuExpanded { get; set; }
    public bool MissingChartSeries { get; set; }
    public bool ChartAllData { get; set; }

    public List<ChartSeriesDefinition>? ChartSeriesList { get; set; }
}

public class SuggestedChartPresetModelWithVariants : SuggestedChartPresetModel
{
    public List<SuggestedChartPresetModel>? Variants { get; set; }
}
