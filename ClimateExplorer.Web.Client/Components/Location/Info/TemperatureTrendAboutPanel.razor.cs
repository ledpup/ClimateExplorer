namespace ClimateExplorer.Web.Client.Components.Info;

using System.Globalization;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.Client.Units;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The temperature trend modal's "About trends" side panel: the shared overview of what a trend is,
/// plus the full statistical breakdown for each window - including the ones that weren't significant,
/// which is where a user goes to find out why the dashboard shows the trend it does.
/// </summary>
public partial class TemperatureTrendAboutPanel
{
    private const string OverviewTabName = "Overview";
    private const string DataTypeLabel = "Temperature";
    private const UnitOfMeasure Unit = UnitOfMeasure.DegreesCelsius;

    private SidePanel? sidePanel;
    private string selectedTab = OverviewTabName;

    [Parameter]
    public Location? Location { get; set; }

    [Parameter]
    [EditorRequired]
    public LocationTrendDetails? Details { get; set; }

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    [Inject]
    private IExporter Exporter { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private NavigationManager NavManager { get; set; } = default!;

    [Inject]
    private ILogger<TemperatureTrendAboutPanel> Logger { get; set; } = default!;

    private IReadOnlyList<LocationTrendWindow> Windows => Details?.Windows ?? [];

    private LocationTrendWindow? SelectedWindow => Windows.FirstOrDefault(x => TabName(x) == selectedTab);

    public Task Show()
    {
        return sidePanel!.ShowAsync();
    }

    private static string TabName(LocationTrendWindow window) => window.Window.ToString();

    private string DescribeWindow(LocationTrendWindow window)
    {
        var years = $"{window.FirstYear.ToString(CultureInfo.InvariantCulture)}-{window.LastYear.ToString(CultureInfo.InvariantCulture)}";
        var rate = TrendFormatting.FormatPerDecadeValue(window.Regression, window.Regression.Input.MaximumX, Unit, Units);

        return window.IsSignificant
            ? $"{TrendWindowLabel.Get(window.Window)} ({years}): {rate}."
            : $"{TrendWindowLabel.Get(window.Window)} ({years}): the fitted rate is {rate}, but it isn't statistically significant.";
    }

    private IReadOnlyList<TrendStatSection> BuildSections(LocationTrendWindow window)
    {
        return TrendStatSectionBuilder.Build(new TrendStatSubject(DataTypeLabel, Unit), window.Regression, window.Points, Details!.FullPeriodPoints);
    }

    private void OnTabChanged(string tabName)
    {
        selectedTab = tabName;
    }

    private async Task DownloadAsync(LocationTrendWindow window)
    {
        if (Location is null)
        {
            return;
        }

        var windowLabel = TrendWindowLabel.Get(window.Window);
        var fileStream = Exporter.ExportTrendData(Logger, Location, DataTypeLabel, windowLabel, window.Points, NavManager.Uri, Unit);
        var fileName = $"{Location.Name}-{DataTypeLabel}-{windowLabel}-trend-data.csv";

        using var streamRef = new DotNetStreamReference(stream: fileStream);
        await JsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
    }
}
