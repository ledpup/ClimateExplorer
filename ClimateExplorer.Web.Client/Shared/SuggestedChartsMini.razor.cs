namespace ClimateExplorer.Web.Client.Shared;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

public partial class SuggestedChartsMini
{
    [Parameter]
    public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    [Parameter]
    public Location? SelectedLocation { get; set; }

    [Parameter]
    public EventCallback<SuggestedChartPresetModel> OnChartPresetSelected { get; set; }

    [Parameter]
    public EventCallback OnClickCollapser { get; set; }

    private List<SuggestedChartPresetModelWithVariants>? SuggestedPresets { get; set; }

    protected override void OnParametersSet()
    {
        if (DataSetDefinitions == null || SelectedLocation == null)
        {
            return;
        }

        SuggestedPresets = SuggestedPresetLists.LocationBasedPresetsMini(DataSetDefinitions, SelectedLocation!);

        // Presets may call for data that is not available at all locations. In those
        // cases, remove the ChartSeries entries that aren't available & set a flag
        // on the preset so that an indicator can be shown to the user.
        foreach (var p in SuggestedPresets!.Where(x => x.ChartSeriesList != null))
        {
            p.MissingChartSeries = false;

            foreach (var csd in p.ChartSeriesList!.ToArray())
            {
                if (!csd.SourceSeriesSpecifications!.Any() || csd.SourceSeriesSpecifications!.Any(x => x.MeasurementDefinition == null))
                {
                    p.ChartSeriesList.Remove(csd);

                    p.MissingChartSeries = true;
                }
            }

            if (p.Variants != null)
            {
                foreach (var v in p.Variants)
                {
                    v.MissingChartSeries = false;

                    foreach (var csd in v.ChartSeriesList!.ToArray())
                    {
                        if (!csd.SourceSeriesSpecifications!.Any() || csd.SourceSeriesSpecifications!.Any(x => x.MeasurementDefinition == null))
                        {
                            v.ChartSeriesList.Remove(csd);

                            v.MissingChartSeries = true;
                        }
                    }
                }
            }
        }
    }

    private string CalculateClassForSuggestedDataSetElement(SuggestedChartPresetModel s)
    {
        string c = "suggested-data-set";

        if (s.MenuExpanded)
        {
            c += " menu-expanded";
        }

        return c;
    }
}
