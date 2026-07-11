namespace ClimateExplorer.Web.Client.Components;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Web.UiModel.SuggestedPresetLists;

public partial class SuggestedCharts
{
    [Parameter]
    public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    [Parameter]
    public Core.Model.Location? SelectedLocation { get; set; }

    [Parameter]
    public EventCallback<SuggestedChartPresetModel> OnChartPresetSelected { get; set; }

    [Parameter]
    public bool ExpandUpwards { get; set; }

    [Parameter]
    public PresetTypes PresetType { get; set; }

    private List<SuggestedChartPresetModelWithVariants>? SuggestedPresets { get; set; }

    protected override void OnParametersSet()
    {
        if (DataSetDefinitions == null)
        {
            return;
        }

        // The location-based preset lists dereference SelectedLocation. The page can render this
        // component before the location has resolved (e.g. the home page before its fallback
        // location loads), so guard against null rather than assuming it's always set.
        if (SelectedLocation == null && PresetType != PresetTypes.Global)
        {
            SuggestedPresets = null;
            return;
        }

        switch (PresetType)
        {
            case PresetTypes.Location:
                SuggestedPresets = SuggestedPresetLists.LocationBasedPresets(DataSetDefinitions, SelectedLocation!);
                break;
            case PresetTypes.Global:
                SuggestedPresets = SuggestedPresetLists.GlobalPresets(DataSetDefinitions);
                break;
            case PresetTypes.MobileLocation:
                SuggestedPresets = SuggestedPresetLists.LocationBasedPresetsMobile(DataSetDefinitions, SelectedLocation!);
                break;
        }

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
                else if (csd.MinimumDataResolution != null && csd.SourceSeriesSpecifications!.Any(x => x.MeasurementDefinition!.DataResolution < csd.MinimumDataResolution))
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
                        else if (csd.MinimumDataResolution != null && csd.SourceSeriesSpecifications!.Any(x => x.MeasurementDefinition!.DataResolution < csd.MinimumDataResolution))
                        {
                            v.ChartSeriesList.Remove(csd);

                            v.MissingChartSeries = true;
                        }
                    }
                }
            }
        }
    }

    private async Task OnPresetSelected(SuggestedChartPresetModel m)
    {
        m.MenuExpanded = false;
        await OnChartPresetSelected.InvokeAsync(m);
    }

    private string CalculateClassForSuggestedDataSetElement(SuggestedChartPresetModel s)
    {
        string c = "suggested-data-set";

        if (s.MenuExpanded)
        {
            c += " menu-expanded";
        }

        if (ExpandUpwards)
        {
            c += " expand-upwards";
        }

        return c;
    }
}
