namespace ClimateExplorer.Web.Client.Components;

using Microsoft.AspNetCore.Components;

public partial class HomeOverviewInfo
{
    [Parameter]
    public string? LocationName { get; set; }

    [Parameter]
    public bool ShowLocationPrompt { get; set; } = true;

    [Parameter]
    public bool NearMeLoading { get; set; }

    [Parameter]
    public string? NearMeErrorMessage { get; set; }

    [Parameter]
    public EventCallback OnNearMe { get; set; }

    [Parameter]
    public EventCallback OnAddDataSet { get; set; }

    [Parameter]
    public EventCallback OnToggleSuggestedCharts { get; set; }

    [Parameter]
    public EventCallback OnShowRecordHigh { get; set; }

    [Parameter]
    public EventCallback OnShowRecentObservations { get; set; }
}
