namespace ClimateExplorer.Web.Client.Shared.Info;

using Microsoft.AspNetCore.Components;

public partial class HomeOverviewInfo
{
    [Parameter]
    public EventCallback OnAddDataSet { get; set; }

    [Parameter]
    public EventCallback OnToggleSuggestedCharts { get; set; }

    [Parameter]
    public EventCallback OnShowRecordHigh { get; set; }
}
