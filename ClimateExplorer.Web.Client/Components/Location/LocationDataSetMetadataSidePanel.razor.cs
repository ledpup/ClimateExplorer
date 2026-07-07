namespace ClimateExplorer.Web.Client.Components.Location;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;

public partial class LocationDataSetMetadataSidePanel
{
    private SidePanel? sidePanel;
    private Location? selectedLocation;
    private IReadOnlyList<DataSetMetadata>? metadata;
    private bool isLoading;
    private string? errorMessage;
    private string? activeTabKey;
    private int loadRequestId;

    [Inject]
    public IDataService? DataService { get; set; }

    [Inject]
    public ILogger<LocationDataSetMetadataSidePanel>? Logger { get; set; }

    [Parameter]
    public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    [Parameter]
    public string Width { get; set; } = "85%";

    private string Title => selectedLocation is null
        ? "Data sources"
        : $"Data sources for {selectedLocation.Name}";

    private DataSetMetadata? ActiveMetadata => metadata?
        .Select((row, index) => new { Row = row, Key = BuildTabKey(row, index) })
        .FirstOrDefault(x => x.Key == activeTabKey)
        ?.Row;

    private DataSetDefinitionViewModel? ActiveDataSetDefinition => ActiveMetadata?.DataSetDefinitionId is null
        ? null
        : DataSetDefinitions?.FirstOrDefault(x => x.Id == ActiveMetadata.DataSetDefinitionId);

    public async Task ShowAsync(Location location)
    {
        selectedLocation = location;
        await LoadAsync(showPanel: true);
    }

    private async Task RetryAsync()
    {
        await LoadAsync(showPanel: false);
    }

    private async Task LoadAsync(bool showPanel)
    {
        if (selectedLocation is null)
        {
            return;
        }

        var requestId = ++loadRequestId;
        isLoading = true;
        errorMessage = null;
        metadata = null;
        activeTabKey = null;

        if (showPanel)
        {
            await (sidePanel?.ShowAsync() ?? Task.CompletedTask);
        }

        try
        {
            var result = await DataService!.GetLocationDataSetMetadata(selectedLocation.Id);
            if (requestId != loadRequestId)
            {
                return;
            }

            metadata = result;
            activeTabKey = metadata.Count > 0 ? BuildTabKey(metadata[0], 0) : null;
        }
        catch (Exception ex)
        {
            if (requestId != loadRequestId)
            {
                return;
            }

            Logger!.LogError(ex, "Unable to load dataset metadata for location {LocationId}", selectedLocation.Id);
            errorMessage = "Unable to load dataset metadata.";
        }
        finally
        {
            if (requestId == loadRequestId)
            {
                isLoading = false;
                StateHasChanged();
            }
        }
    }

    private Task OnTabChanged(string tabKey)
    {
        activeTabKey = tabKey;
        return Task.CompletedTask;
    }

    private string BuildTabKey(DataSetMetadata row, int index)
    {
        return row.DataSetDefinitionId?.ToString() ?? $"dataset-{index}";
    }

    private string GetTabLabel(DataSetMetadata row, int index)
    {
        if (!string.IsNullOrWhiteSpace(row.SourceCode))
        {
            return row.SourceCode;
        }

        if (!string.IsNullOrWhiteSpace(row.SourceName))
        {
            return row.SourceName;
        }

        return $"Dataset {index + 1}";
    }
}
