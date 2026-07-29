namespace ClimateExplorer.Web.Client.Pages;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class About
{
    private ApiMetadataModel? apiMetadata;
    private ElementReference contentElement;
    private List<TocItem> tocItems = [];
    private List<PublisherGroup> dataSourceGroups = [];
    private bool pipelineModalVisible = false;

    [Inject]
    public IDataService? DataService { get; set; }

    [Inject]
    public IJSRuntime? JsRuntime { get; set; }

    protected string? Ogtitle { get; set; }

    protected string? Ogurl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Ogtitle = $"About ClimateExplorer";
        Ogurl = $"https://climateexplorer.net/about";

        if (DataService == null)
        {
            throw new NullReferenceException(nameof(DataService));
        }

        apiMetadata = await DataService.GetAbout();

        var dataSetDefinitions = await DataService.GetDataSetDefinitions();
        dataSourceGroups = BuildDataSourceGroups(dataSetDefinitions);

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await BuildTocAsync();
            StateHasChanged();
        }
    }

    private static List<PublisherGroup> BuildDataSourceGroups(IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions)
    {
        return
            dataSetDefinitions
            .Where(x => x.Publisher != null)
            .GroupBy(x => x.Publisher!)
            .Select(publisherGroup =>
                new PublisherGroup
                {
                    Publisher = publisherGroup.Key,
                    PublisherUrl = publisherGroup.Select(x => x.PublisherUrl).FirstOrDefault(x => x != null),
                    Divisions =
                        publisherGroup
                        .GroupBy(x => x.PublisherDivision)
                        .Select(divisionGroup =>
                            new DivisionGroup
                            {
                                Name = divisionGroup.Key,
                                PublisherDescription = divisionGroup.Select(x => x.PublisherDescription).FirstOrDefault(x => x != null),
                                Items = divisionGroup.ToList(),
                            })
                        .ToList(),
                })
            .ToList();
    }

    private async Task BuildTocAsync()
    {
        if (JsRuntime == null)
        {
            return;
        }

        var headings = await JsRuntime.InvokeAsync<List<HeadingInfo>>("getMainHeadings");

        string? currentParent = null;
        foreach (var heading in headings)
        {
            var text = heading.Text;
            var id = text.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("'", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty);

            if (heading.Index < 3)
            {
                currentParent = text;
            }

            tocItems.Add(new TocItem
            {
                Text = text,
                Href = $"#{id}",
                ParentSection = heading.Index >= 3 ? currentParent : null,
            });

            await JsRuntime.InvokeVoidAsync("addIdToHeading", heading.Index, id);
        }
    }

    private void ShowPipelineModal()
    {
        pipelineModalVisible = true;
    }

    private class TocItem
    {
        public required string Text { get; set; }
        public required string Href { get; set; }
        public string? ParentSection { get; set; }
    }

    private class HeadingInfo
    {
        public required string Text { get; set; }
        public int Index { get; set; }
    }

    private class PublisherGroup
    {
        public required string Publisher { get; set; }
        public string? PublisherUrl { get; set; }
        public required List<DivisionGroup> Divisions { get; set; }
    }

    private class DivisionGroup
    {
        public string? Name { get; set; }
        public string? PublisherDescription { get; set; }
        public required List<DataSetDefinitionViewModel> Items { get; set; }
    }
}
