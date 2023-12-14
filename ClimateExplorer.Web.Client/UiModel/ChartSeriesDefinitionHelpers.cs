namespace ClimateExplorer.Web.UiModel;

public static class ChartSeriesDefinitionHelpers
{
    public static List<ChartSeriesDefinition> CreateNewListWithoutDuplicates(this List<ChartSeriesDefinition> csds)
    {
        return csds.Distinct(new ChartSeriesDefinition.ChartSeriesDefinitionComparer()).ToList();
    }
}
