namespace ClimateExplorer.Web.Client.Components.ChartSeries;

using Blazorise;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using static ClimateExplorer.Core.Enums;

public partial class ChartSeriesView
{
    private Validation? validation;
    private AboutData? aboutData;

    public enum ChartSeriesTitleStyle
    {
        WholeTitleBarInSeriesColour,
        OnlyIndicatorAndTextInSeriesColour,
    }

    [Parameter]
    public ChartSeriesDefinition? ChartSeries { get; set; }

    [Parameter]
    public IReadOnlyList<DataSetMetadata>? SourceMetadata { get; set; }

    [Parameter]
    public EventCallback OnSeriesChanged { get; set; }

    [Parameter]
    public EventCallback<ChartSeriesDefinition> OnDuplicateSeries { get; set; }

    [Parameter]
    public EventCallback<ChartSeriesDefinition> OnRemoveSeries { get; set; }

    [Parameter]
    public ChartSeriesTitleStyle TitleStyle { get; set; }

    private string StyleForTitleBar => GenerateStyleForTitleBar();

    private string StyleForOuterDiv => GenerateStyleForOuterDiv();

    private string ToggleExpandedLabel => ChartSeries?.IsExpanded == true
        ? "Collapse series options"
        : "Expand series options";

    public string GenerateStyleForOuterDiv()
    {
        if (TitleStyle == ChartSeriesTitleStyle.WholeTitleBarInSeriesColour)
        {
            return "--series-colour: " + ChartSeries!.Colour + ";";
        }

        return string.Empty;
    }

    public string GenerateStyleForColourIndicator()
    {
        return "background-color: " + ChartSeries!.Colour;
    }

    public string GenerateStyleForTitleBar()
    {
        switch (TitleStyle)
        {
            case ChartSeriesTitleStyle.WholeTitleBarInSeriesColour:
                return "color: #425f59; border-left: solid 12px " + ChartSeries!.Colour + ";";
            case ChartSeriesTitleStyle.OnlyIndicatorAndTextInSeriesColour:
                return "color: " + ChartSeries!.Colour;
            default:
                throw new NotImplementedException($"TitleStyle {TitleStyle}");
        }
    }

    private bool ShouldDisableAggregationOptions(ChartSeriesDefinition csd)
    {
        return csd.SeriesDerivationType == SeriesDerivationTypes.AverageOfAnomaliesInRegion;
    }

    private bool ShouldDisableSmoothingWindow(ChartSeriesDefinition csd)
    {
        return csd.Smoothing != SeriesSmoothingOptions.MovingAverage;
    }

    private bool ShouldDisableDisplay(ChartSeriesDefinition csd)
    {
        return csd.SeriesDerivationType == SeriesDerivationTypes.AverageOfAnomaliesInRegion;
    }

    private bool ShouldDisableTransformation(ChartSeriesDefinition csd)
    {
        return csd.SeriesDerivationType == SeriesDerivationTypes.AverageOfAnomaliesInRegion;
    }

    private async Task OnAboutThisDataClicked()
    {
        await aboutData!.Show();
    }

    private async Task OnDuplicateSeriesClicked()
    {
        await OnDuplicateSeries.InvokeAsync(ChartSeries);
    }

    private async Task OnRemoveSeriesClicked()
    {
        await OnRemoveSeries.InvokeAsync(ChartSeries);
    }

    private async Task OnAggregationChanged(SeriesAggregationOptions o)
    {
        ChartSeries!.Aggregation = o;

        await OnSeriesChanged.InvokeAsync();
    }

    private async Task OnSmoothingChanged(SeriesSmoothingOptions o)
    {
        ChartSeries!.Smoothing = o;

        await OnSeriesChanged.InvokeAsync();
    }

    private async Task OnSecondaryCalculationChanged(SecondaryCalculationOptions o)
    {
        ChartSeries!.SecondaryCalculation = o;

        await OnSeriesChanged.InvokeAsync();
    }

    private async Task OnSmoothingWindowChanged(int w)
    {
        ChartSeries!.SmoothingWindow = w;

        await OnSeriesChanged.InvokeAsync();
    }

    private async Task OnValueChanged(SeriesValueOptions o)
    {
        ChartSeries!.Value = o;

        await OnSeriesChanged.InvokeAsync();
    }

    private async Task OnDisplayStyleChanged(SeriesDisplayStyle s)
    {
        ChartSeries!.DisplayStyle = s;

        await OnSeriesChanged.InvokeAsync();
    }

    private async Task OnColourChanged(Colours c)
    {
        ChartSeries!.RequestedColour = c;

        await OnSeriesChanged.InvokeAsync();
    }

    private async Task OnTransformationChanged(SeriesTransformations s)
    {
        ChartSeries!.SeriesTransformation = s;

        await OnSeriesChanged.InvokeAsync();
    }

    private void OnIsLockedChanged(bool val)
    {
        ChartSeries!.IsLocked = val;
    }

    private async Task OnShowTrendlineChanged(bool val)
    {
        ChartSeries!.ShowTrendline = val;

        await OnSeriesChanged.InvokeAsync();
    }

    private string GetColourName(Colours colour)
    {
        return colour switch
        {
            Colours.AutoAssigned => "Automatic",
            _ => colour.ToString(),
        };
    }

    private async Task OnCustomTransformationChanged(string value)
    {
        ChartSeries!.CustomTransformation = value;
        if (validation is not null)
        {
            var validationStatus = validation.Validate();
            if (validationStatus == ValidationStatus.Success)
            {
                await OnSeriesChanged.InvokeAsync();
            }
        }
    }

    private void ValidateCustomTransformation(ValidatorEventArgs e)
    {
        try
        {
            if (e.Value is null)
            {
                e.Status = ValidationStatus.None;
                return;
            }

            CustomTransformationParser.Parse(Convert.ToString(e.Value)!);
            e.Status = ValidationStatus.Success;
        }
        catch
        {
            e.Status = ValidationStatus.Error;
        }
    }

    private void ExpandCollapse()
    {
        ChartSeries!.IsExpanded = !ChartSeries.IsExpanded;
    }

    private void OnTitleBarKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Enter" or " ")
        {
            ExpandCollapse();
        }
    }
}
