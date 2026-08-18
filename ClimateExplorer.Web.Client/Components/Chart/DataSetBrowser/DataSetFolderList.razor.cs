namespace ClimateExplorer.Web.Client.Components.Chart.DataSetBrowser;

using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Renders a flat list of data set library folders, each with its data set entries and an "add" action.
/// Shared by <see cref="LocalDataSetBrowser"/> and <see cref="GlobalDataSetBrowser"/>.
/// </summary>
public partial class DataSetFolderList
{
    [Parameter]
    public List<DataSetLibraryFolder>? RootFolders { get; set; }

    [Parameter]
    public EventCallback<DataSetLibraryEntry> OnAddDataSet { get; set; }

    private async Task AddDataSet(DataSetLibraryEntry dle)
    {
        await OnAddDataSet.InvokeAsync(dle);
    }
}
