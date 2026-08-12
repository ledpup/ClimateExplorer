namespace ClimateExplorer.Web.Client.Components.Chart;

using System.Globalization;
using Blazorise;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Components;
using ClimateExplorer.Web.Client.Components.Chart.Trend;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using static ClimateExplorer.Core.Enums;

public partial class ChartSeriesView
{
    private Validation? validation;
    private Validation? predictionYearsValidation;
    private AboutData? aboutData;
    private ChartTrendPanel? trendPanel;
    private string predictionYearsText = string.Empty;

    public enum ChartSeriesTitleStyle
    {
        WholeTitleBarInSeriesColour,
        OnlyIndicatorAndTextInSeriesColour,
    }

    [Parameter]
    public ChartSeriesDefinition? ChartSeries { get; set; }

    [Parameter]
    public IReadOnlyList<DataSetMetadata>? SourceMetadata { get; set; }

    /// <summary>
    /// The trend windows fitted for this series on the last chart build, or null when the trend
    /// module is off or the build hasn't produced them yet. The dropdown can only offer periods
    /// that came back statistically significant, which is why the control needs the result rather
    /// than just the request.
    /// </summary>
    [Parameter]
    public ChartSeriesTrend? Trend { get; set; }

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

    /// <summary>
    /// A trend regresses a value against a calendar year and projects one value per year, so the
    /// module only makes sense while the x-axis is years.
    /// </summary>
    private bool IsTrendModuleAvailable => ChartSeries?.BinGranularity == BinGranularities.ByYear;

    private IReadOnlyList<TrendWindow> SelectableTrendPeriods => Trend?.SignificantWindows ?? [];

    private TrendWindow SelectedTrendPeriod =>
        ChartSeries?.TrendPeriod is { } period && SelectableTrendPeriods.Contains(period)
            ? period
            : SelectableTrendPeriods[0];

    /// <summary>
    /// What the disabled dropdown says when there is nothing to select - either the trends haven't
    /// been fitted yet, or none of the three periods cleared the significance threshold.
    /// </summary>
    private string UnavailableTrendPeriodValue => Trend is null
        ? "Calculating…"
        : Trend.UnavailableReason is not null
            ? "Not enough data"
            : "No significant trend";

    private string YearsToPredictTooltip =>
        $"How many years past the end of the data to project the trend ({TrendPredictionRange.Minimum}-{TrendPredictionRange.Maximum})";

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

    protected override void OnParametersSet()
    {
        if (ChartSeries is null)
        {
            return;
        }

        // Keep the text box in step with the definition, including when a value arriving from a URL
        // has been clamped into range. The input commits on blur rather than per keystroke, so this
        // doesn't fight with typing.
        if (!int.TryParse(predictionYearsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var current)
            || current != ChartSeries.TrendPredictionYears)
        {
            predictionYearsText = ChartSeries.TrendPredictionYears.ToString(CultureInfo.InvariantCulture);
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

    private async Task OnShowTrendChanged(bool val)
    {
        ChartSeries!.ShowTrend = val;

        if (!val)
        {
            // Clearing the period as well means switching the module back on later starts from the
            // default "best available window" rather than a stale choice.
            ChartSeries.TrendPeriod = null;
        }

        await OnSeriesChanged.InvokeAsync();
    }

    private async Task OnTrendPeriodChanged(TrendWindow window)
    {
        ChartSeries!.TrendPeriod = window;

        await OnSeriesChanged.InvokeAsync();
    }

    private async Task OnPredictionYearsChanged(string value)
    {
        predictionYearsText = value;

        if (predictionYearsValidation is null || predictionYearsValidation.Validate() != ValidationStatus.Success)
        {
            return;
        }

        var years = int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

        if (years == ChartSeries!.TrendPredictionYears)
        {
            return;
        }

        // Only the projected points depend on this - the regression and its significance don't - so
        // the chart just rebuilds; the user is never re-told about a period they can't select.
        ChartSeries.TrendPredictionYears = years;

        await OnSeriesChanged.InvokeAsync();
    }

    private void ValidatePredictionYears(ValidatorEventArgs e)
    {
        var text = Convert.ToString(e.Value);

        e.Status = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var years)
            && TrendPredictionRange.IsValid(years)
                ? ValidationStatus.Success
                : ValidationStatus.Error;
    }

    private async Task OnAboutTrendsClicked()
    {
        await trendPanel!.Show();
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
