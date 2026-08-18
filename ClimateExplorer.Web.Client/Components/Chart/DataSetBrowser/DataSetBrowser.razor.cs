namespace ClimateExplorer.Web.Client.Components.Chart.DataSetBrowser;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Shell that links to <see cref="LocalDataSetBrowser"/> and <see cref="GlobalDataSetBrowser"/>. When a
/// current location is supplied, both are offered behind a Local/Global tab control. Otherwise (e.g. when
/// opened from the global page) only the global data sets are shown, with no tabs.
/// </summary>
public partial class DataSetBrowser
{
    [Parameter]
    public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    [Parameter]
    public EventCallback<DataSetLibraryEntry> OnAddDataSet { get; set; }

    [Parameter]
    public Location? CurrentLocation { get; set; }

    [Parameter]
    public Location? PreviousLocation { get; set; }

    private string ActiveTab { get; set; } = "Local";

    private void OnTabChanged(string tabName)
    {
        ActiveTab = tabName;
    }
}
