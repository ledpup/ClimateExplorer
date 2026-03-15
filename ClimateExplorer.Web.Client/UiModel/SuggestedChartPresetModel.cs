namespace ClimateExplorer.Web.UiModel;

public class SuggestedChartPresetModel
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool MenuExpanded { get; set; }
    public bool MissingChartSeries { get; set; }
    public bool ChartAllData { get; set; }
    public short? StartYear { get; set; }
    public short? EndYear { get; set; }

    public List<ChartSeriesDefinition>? ChartSeriesList { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Not important")]
public class SuggestedChartPresetModelWithVariants : SuggestedChartPresetModel
{
    public List<SuggestedChartPresetModel>? Variants { get; set; }
}
